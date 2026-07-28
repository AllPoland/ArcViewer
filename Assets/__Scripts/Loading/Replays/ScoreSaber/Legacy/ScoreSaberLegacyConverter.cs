using System.Collections.Generic;
using UnityEngine;

public static class ScoreSaberLegacyConverter
{
    public static bool NeedsConversion(Replay replay)
    {
        return replay.scoreSaberLegacyScoreData != null && !replay.scoreSaberLegacyConverted;
    }

    public static void Convert(Replay replay, BeatmapDifficulty difficulty)
    {
        List<LegacyScoreFrame> legacyData = replay.scoreSaberLegacyScoreData;
        if(legacyData == null || legacyData.Count == 0) return;

        List<ComboChangePoint> comboChanges = FindComboChanges(legacyData);
        List<LegacyNoteAction> actions = ComputeNoteActions(legacyData, comboChanges);
        (List<NoteEvent> syntheticNotes, int missCount, int badCutCount) = MatchActionsToNotes(actions, difficulty, replay.frames);

        replay.notes = syntheticNotes;
        replay.scoreSaberLegacyHUDData = BuildHUDData(difficulty);
        replay.scoreSaberLegacyConverted = true;

        Debug.Log($"Generated {syntheticNotes.Count} synthetic note events for legacy replay ({missCount} misses, {badCutCount} bad cuts detected).");
    }

    private static List<ComboChangePoint> FindComboChanges(List<LegacyScoreFrame> legacyData)
    {
        List<ComboChangePoint> changes = new List<ComboChangePoint>();
        for(int i = 1; i < legacyData.Count; i++)
        {
            int prev = legacyData[i - 1].combo;
            int curr = legacyData[i].combo;
            if(curr == prev) continue;

            changes.Add(new ComboChangePoint
            {
                frameIndex = i,
                time = legacyData[i].time,
                isMiss = curr < prev,
                hitCount = curr > prev ? curr - prev : curr
            });
        }
        return changes;
    }

    // post swing settles over 50 to 100ms. windows narrower than this
    // miss deferred post swing and need chaining. 40ms catches stacks
    // and sliders while excluding fast streams which have enough
    // settled post swing
    private const float NarrowWindowThreshold = 0.04f;

    private static List<LegacyNoteAction> ComputeNoteActions(
        List<LegacyScoreFrame> legacyData, List<ComboChangePoint> comboChanges)
    {
        List<LegacyNoteAction> actions = new List<LegacyNoteAction>();
        byte comboMult = 0, comboProgress = 0;

        for(int c = 0; c < comboChanges.Count;)
        {
            ComboChangePoint change = comboChanges[c];

            if(change.isMiss)
            {
                if(comboMult > 0) comboMult--;
                comboProgress = 0;
                actions.Add(new LegacyNoteAction { time = change.time, isHit = false, baseScore = 0 });
                c++;
                continue;
            }

            if(change.hitCount == 0)
            {
                c++;
                continue;
            }

            // chain consecutive hits where gaps are too narrow for
            // post swing to settle between them
            int chainEnd = c + 1;
            while(chainEnd < comboChanges.Count
                && !comboChanges[chainEnd].isMiss
                && comboChanges[chainEnd].hitCount > 0
                && comboChanges[chainEnd].time - comboChanges[chainEnd - 1].time < NarrowWindowThreshold)
            {
                chainEnd++;
            }

            int totalHits = 0;
            for(int g = c; g < chainEnd; g++)
                totalHits += comboChanges[g].hitCount;

            if(totalHits == 1)
            {
                // single note with a wide window
                int scoreBefore = legacyData[change.frameIndex - 1].score;
                int scoreEnd = chainEnd < comboChanges.Count
                    ? legacyData[comboChanges[chainEnd].frameIndex - 1].score
                    : legacyData[legacyData.Count - 1].score;

                ScoringUtils.ManuallyAdvanceCombo(ref comboMult, ref comboProgress);
                int mult = Mathf.Max(1, ScoringUtils.ComboMultipliers[comboMult]);
                int baseScore = Mathf.Clamp((scoreEnd - scoreBefore) / mult, 0, ScoringUtils.MaxNoteScore);
                actions.Add(new LegacyNoteAction { time = change.time, isHit = true, baseScore = baseScore });
            }
            else
            {
                // multiple notes in a narrow window. use a settled
                // window spanning the chain and distribute evenly
                // with data driven variation
                int scoreBefore = legacyData[comboChanges[c].frameIndex - 1].score;
                int settledIndex = chainEnd < comboChanges.Count
                    ? comboChanges[chainEnd].frameIndex - 1
                    : legacyData.Count - 1;
                int totalScore = legacyData[settledIndex].score - scoreBefore;

                // sum multipliers for equal distribution
                byte tempMult = comboMult, tempProg = comboProgress;
                int multSum = 0;
                for(int g = c; g < chainEnd; g++)
                    for(int h = 0; h < comboChanges[g].hitCount; h++)
                    {
                        ScoringUtils.ManuallyAdvanceCombo(ref tempMult, ref tempProg);
                        multSum += ScoringUtils.ComboMultipliers[tempMult];
                    }

                int avgBase = Mathf.Clamp(
                    Mathf.RoundToInt((float)totalScore / Mathf.Max(1, multSum)),
                    0, ScoringUtils.MaxNoteScore);

                // immediate deltas guide variation so same beat
                // notes dont all show identical scores
                float[] perNoteDeltas = new float[chainEnd - c];
                float deltaSum = 0f;
                for(int g = c; g < chainEnd; g++)
                {
                    int delta = legacyData[comboChanges[g].frameIndex].score
                        - legacyData[comboChanges[g].frameIndex - 1].score;
                    float perNote = Mathf.Max(1f, delta) / comboChanges[g].hitCount;
                    perNoteDeltas[g - c] = perNote;
                    deltaSum += perNote * comboChanges[g].hitCount;
                }

                float avgDelta = deltaSum / totalHits;

                for(int g = c; g < chainEnd; g++)
                {
                    // clamped deviation from average based on real score data
                    float ratio = perNoteDeltas[g - c] / Mathf.Max(1f, avgDelta);
                    int deviation = Mathf.Clamp(Mathf.RoundToInt((ratio - 1f) * 15f), -5, 5);

                    for(int h = 0; h < comboChanges[g].hitCount; h++)
                    {
                        ScoringUtils.ManuallyAdvanceCombo(ref comboMult, ref comboProgress);
                        // for multi hit changes we only have one delta
                        // so alternate between notes within the change
                        int multiOffset = comboChanges[g].hitCount > 1 ? (h % 2 == 0 ? -1 : 1) : 0;
                        int baseScore = Mathf.Clamp(avgBase + deviation + multiOffset, 0, ScoringUtils.MaxNoteScore);
                        actions.Add(new LegacyNoteAction { time = comboChanges[g].time, isHit = true, baseScore = baseScore });
                    }
                }
            }

            c = chainEnd;
        }
        return actions;
    }

    private static (List<NoteEvent>, int, int) MatchActionsToNotes(
        List<LegacyNoteAction> actions, BeatmapDifficulty difficulty, List<Frame> frames)
    {
        int noteCount = difficulty.Notes.Length;
        float[] noteTimes = new float[noteCount];
        int[] noteIDs = new int[noteCount];
        int[] noteColors = new int[noteCount];
        int[] noteCutDirs = new int[noteCount];
        bool[] matched = new bool[noteCount];

        for(int i = 0; i < noteCount; i++)
        {
            BeatmapColorNote note = difficulty.Notes[i];
            noteTimes[i] = TimeManager.TimeFromBeat(note.b);
            noteIDs[i] = ((int)ScoringType.Note * 10000) + (note.x * 1000) + (note.y * 100) + (note.c * 10) + note.d;
            noteColors[i] = note.c;
            noteCutDirs[i] = note.d;
        }

        List<NoteEvent> syntheticNotes = new List<NoteEvent>();
        int missCount = 0, badCutCount = 0;

        foreach(LegacyNoteAction action in actions)
        {
            int bestIndex = FindClosestNote(noteTimes, noteCutDirs, matched, noteCount, action.time, action.isHit);
            if(bestIndex < 0) continue;

            matched[bestIndex] = true;

            if(action.isHit)
            {
                syntheticNotes.Add(new NoteEvent
                {
                    noteID = noteIDs[bestIndex],
                    eventTime = noteTimes[bestIndex],
                    spawnTime = noteTimes[bestIndex],
                    eventType = NoteEventType.good,
                    noteCutInfo = BuildCutInfo(action.baseScore, noteColors[bestIndex])
                });
            }
            else if(DetectBadCut(frames, noteTimes[bestIndex], noteColors[bestIndex], noteCutDirs[bestIndex]))
            {
                badCutCount++;
                syntheticNotes.Add(new NoteEvent
                {
                    noteID = noteIDs[bestIndex],
                    eventTime = noteTimes[bestIndex],
                    spawnTime = noteTimes[bestIndex],
                    eventType = NoteEventType.bad,
                    noteCutInfo = BuildBadCutInfo(noteColors[bestIndex])
                });
            }
            else
            {
                missCount++;
                syntheticNotes.Add(new NoteEvent
                {
                    noteID = noteIDs[bestIndex],
                    eventTime = noteTimes[bestIndex],
                    spawnTime = noteTimes[bestIndex],
                    eventType = NoteEventType.miss
                });
            }
        }

        for(int i = 0; i < noteCount; i++)
        {
            if(matched[i]) continue;
            syntheticNotes.Add(new NoteEvent
            {
                noteID = noteIDs[i],
                eventTime = noteTimes[i],
                spawnTime = noteTimes[i],
                eventType = NoteEventType.miss
            });
            missCount++;
        }

        syntheticNotes.Sort((a, b) => a.eventTime.CompareTo(b.eventTime));
        return (syntheticNotes, missCount, badCutCount);
    }

    private static int FindClosestNote(
        float[] noteTimes, int[] noteCutDirs, bool[] matched, int noteCount, float actionTime, bool isHit)
    {
        int bestIndex = -1;
        float bestDist = float.MaxValue;

        // for misses prefer directional notes over dots
        if(!isHit)
        {
            for(int i = 0; i < noteCount; i++)
            {
                if(matched[i] || noteCutDirs[i] >= 8) continue;
                float dist = Mathf.Abs(noteTimes[i] - actionTime);
                if(dist < bestDist)
                {
                    bestDist = dist;
                    bestIndex = i;
                }
                else if(dist > bestDist) break;
            }
            if(bestIndex >= 0 && bestDist < 1f) return bestIndex;
        }

        bestIndex = -1;
        bestDist = float.MaxValue;
        for(int i = 0; i < noteCount; i++)
        {
            if(matched[i]) continue;
            float dist = Mathf.Abs(noteTimes[i] - actionTime);
            if(dist < bestDist)
            {
                bestDist = dist;
                bestIndex = i;
            }
            else if(dist > bestDist) break;
        }
        return bestIndex;
    }

    private static NoteCutInfo BuildCutInfo(int baseScore, int colorType)
    {
        int swingScore = Mathf.Max(0, baseScore - 15);
        float preRating, postRating;
        if(swingScore >= ScoringUtils.PreSwingValue)
        {
            preRating = 1f;
            postRating = Mathf.Clamp01((swingScore - ScoringUtils.PreSwingValue) / (float)ScoringUtils.PostSwingValue);
        }
        else
        {
            preRating = swingScore / (float)ScoringUtils.PreSwingValue;
            postRating = 0f;
        }

        return new NoteCutInfo
        {
            speedOK = true, directionOK = true, saberTypeOK = true,
            saberSpeed = 5f, saberDir = Vector3.forward, saberType = colorType,
            cutNormal = Vector3.forward,
            beforeCutRating = preRating, afterCutRating = postRating
        };
    }

    private static readonly float[] CutDirAngles = { 180f, 0f, -90f, 90f, -135f, 135f, -45f, 45f };
    private const float MinSwingSpeed = 1f;
    private const float BadCutAngleThreshold = 45f;
    private const float SwingSearchWindow = 0.25f;

    private static bool DetectBadCut(List<Frame> frames, float noteTime, int colorType, int cutDir)
    {
        if(cutDir < 0 || cutDir > 7 || frames.Count < 2) return false;

        int frameIndex = FindClosestFrame(frames, noteTime);
        if(frameIndex < 0) return false;

        float bestSpeed = 0f;
        Vector3 bestSwingDir = Vector3.zero;
        int searchStart = Mathf.Max(1, frameIndex - 10);
        int searchEnd = Mathf.Min(frames.Count - 1, frameIndex + 10);

        for(int i = searchStart; i <= searchEnd; i++)
        {
            if(Mathf.Abs(frames[i].time - noteTime) > SwingSearchWindow) continue;

            float dt = frames[i].time - frames[i - 1].time;
            if(dt <= 0f) continue;

            Vector3 currPos = colorType == 0 ? frames[i].rightHand.position : frames[i].leftHand.position;
            Vector3 prevPos = colorType == 0 ? frames[i - 1].rightHand.position : frames[i - 1].leftHand.position;
            Vector3 velocity = (currPos - prevPos) / dt;
            float speed = velocity.magnitude;

            if(speed > bestSpeed)
            {
                bestSpeed = speed;
                bestSwingDir = velocity;
            }
        }

        if(bestSpeed < MinSwingSpeed) return false;

        float swingAngle = Mathf.Atan2(bestSwingDir.x, -bestSwingDir.y) * Mathf.Rad2Deg;
        return Mathf.Abs(Mathf.DeltaAngle(swingAngle, CutDirAngles[cutDir])) > BadCutAngleThreshold;
    }

    private static int FindClosestFrame(List<Frame> frames, float time)
    {
        int lo = 0, hi = frames.Count - 1;
        while(lo < hi)
        {
            int mid = (lo + hi) / 2;
            if(frames[mid].time < time) lo = mid + 1;
            else hi = mid;
        }
        if(lo > 0 && Mathf.Abs(frames[lo - 1].time - time) < Mathf.Abs(frames[lo].time - time))
            lo--;
        return lo;
    }

    private static NoteCutInfo BuildBadCutInfo(int colorType)
    {
        return new NoteCutInfo
        {
            speedOK = true, directionOK = false, saberTypeOK = true,
            saberSpeed = 5f, saberDir = Vector3.forward, saberType = colorType,
            cutNormal = Vector3.forward,
            beforeCutRating = 0f, afterCutRating = 0f
        };
    }

    private static LegacyHUDData BuildHUDData(BeatmapDifficulty difficulty)
    {
        LegacyHUDData hudData = new LegacyHUDData();
        int maxScore = 0;
        byte comboMult = 0, comboProgress = 0;

        foreach(BeatmapColorNote note in difficulty.Notes)
        {
            ScoringUtils.ManuallyAdvanceCombo(ref comboMult, ref comboProgress);
            maxScore += ScoringUtils.MaxNoteScore * ScoringUtils.ComboMultipliers[comboMult];
            hudData.maxScores.Add(new LegacyHUDData.MaxScoreFrame
            {
                time = TimeManager.TimeFromBeat(note.b),
                maxScore = maxScore
            });
        }
        return hudData;
    }

    private struct ComboChangePoint
    {
        public int frameIndex;
        public float time;
        public bool isMiss;
        public int hitCount;
    }

    private struct LegacyNoteAction
    {
        public float time;
        public bool isHit;
        public int baseScore;
    }
}


public class LegacyHUDData
{
    public List<MaxScoreFrame> maxScores = new List<MaxScoreFrame>();

    public struct MaxScoreFrame
    {
        public float time;
        public int maxScore;
    }
}
