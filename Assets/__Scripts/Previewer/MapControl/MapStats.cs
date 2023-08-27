using System;
using System.Collections.Generic;
using UnityEngine;

public static class MapStats
{
    private const int roundDigits = 2;

    public static event Action OnStatsUpdated;

    public static int SwingCount { get; private set; }
    public static float SpsPeak16 { get; private set; }
    public static float SpsPeak8 { get; private set; }
    public static float SpsPeak4 { get; private set; }

    public static float NpsPeak16 { get; private set; }
    public static float NpsPeak8 { get; private set; }
    public static float NpsPeak4 { get; private set; }

    public static int NoteCount => noteManager.Objects.Count;
    public static int BombCount => bombManager.Objects.Count;
    public static int WallCount => wallManager.Objects.Count;
    public static int ArcCount => arcManager.Objects.Count;
    public static int ChainCount => chainManager.Objects.Count;
    public static int EventCount => currentBeatmapDifficulty.basicBeatMapEvents.Length + currentBeatmapDifficulty.colorBoostBeatMapEvents.Length;
    public static int BpmEventCount => TimeManager.BpmChanges.Count;

    public static float NotesPerSecond => ((float)NoteCount / effectiveMapLength).Round(roundDigits);
    public static float SwingsPerSecond => ((float)SwingCount / effectiveMapLength).Round(roundDigits);

    private static NoteManager noteManager => ObjectManager.Instance.noteManager;
    private static BombManager bombManager => ObjectManager.Instance.bombManager;
    private static WallManager wallManager => ObjectManager.Instance.wallManager;
    private static ArcManager arcManager => ObjectManager.Instance.arcManager;
    private static ChainManager chainManager => ObjectManager.Instance.chainManager;

    private static Difficulty currentDifficulty => BeatmapManager.CurrentDifficulty;
    private static BeatmapDifficulty currentBeatmapDifficulty => currentDifficulty.beatmapDifficulty;

    private static float songLength => SongManager.GetSongLength();

    private static float startPlayRange = 0f;
    private static float endPlayRange = 0f;
    private static float effectiveMapLength => endPlayRange - startPlayRange;


    public static void UpdateNpsAndSpsValues()
    {
        //The amount of beats where notes are considered to be part of the same swing
        const float sliderPrecision = 1f / 9f;

        //Reset values to re-evaluate
        SwingCount = 0;
        startPlayRange = 0f;
        endPlayRange = songLength;

        int SwingCountPeak16 = 0;
        int SwingCountPeak8 = 0;
        int SwingCountPeak4 = 0;

        int NoteCountPeak16 = 0;
        int NoteCountPeak8 = 0;
        int NoteCountPeak4 = 0;

        //Used to keep track of peak SPS and NPS
        List<float> prevSwings16 = new List<float>();
        List<float> prevSwings8 = new List<float>();
        List<float> prevSwings4 = new List<float>();

        List<float> prevNotes16 = new List<float>();
        List<float> prevNotes8 = new List<float>();
        List<float> prevNotes4 = new List<float>();

        //Need to keep peak windows consistent through BPM changes
        float beatTime = TimeManager.RawTimeFromBeat(1f, TimeManager.BaseBPM);

        List<Note> notes = noteManager.Objects;
        Note previousRedNote = null;
        Note previousBlueNote = null;

        for(int i = 0; i < notes.Count; i++)
        {
            Note currentNote = notes[i];
            bool isRed = currentNote.Color == 0;

            if(i == 0)
            {
                startPlayRange = currentNote.Time;
            }
            else if(i == notes.Count - 1)
            {
                endPlayRange = currentNote.Time;
            }

            Note previousNote = isRed ? previousRedNote : previousBlueNote;
            bool hasPreviousNote = previousNote != null;
            float previousBeat = hasPreviousNote ? previousNote.Beat : Mathf.NegativeInfinity;
            float previousTime = hasPreviousNote ? previousNote.Time : Mathf.NegativeInfinity;

            bool sameAngle = hasPreviousNote
                && ( (currentNote.IsDot || previousNote.IsDot)
                    || previousNote.Angle.Approximately(currentNote.Angle) );

            if(!ObjectManager.CheckSameTime(currentNote.Time, previousTime) && (currentNote.Beat - previousBeat > sliderPrecision || !sameAngle))
            {
                //This note counts as a new swing
                SwingCount++;
                prevSwings16.Add(currentNote.Time);
                prevSwings8.Add(currentNote.Time);
                prevSwings4.Add(currentNote.Time);
            }

            //Every note counts as a note, obviously
            prevNotes16.Add(currentNote.Time);
            prevNotes8.Add(currentNote.Time);
            prevNotes4.Add(currentNote.Time);

            //Clear any notes and swings outside of peak ranges
            prevSwings16.RemoveAllForward(x => x < currentNote.Time - (16 * beatTime));
            prevSwings8.RemoveAllForward(x => x < currentNote.Time - (8 * beatTime));
            prevSwings4.RemoveAllForward(x => x < currentNote.Time - (4 * beatTime));

            prevNotes16.RemoveAllForward(x => x < currentNote.Time - (16 * beatTime));
            prevNotes8.RemoveAllForward(x => x < currentNote.Time - (8 * beatTime));
            prevNotes4.RemoveAllForward(x => x < currentNote.Time - (4 * beatTime));

            //If the new peaks are higher than previous, update them
            SwingCountPeak16 = Mathf.Max(prevSwings16.Count, SwingCountPeak16);
            SwingCountPeak8 = Mathf.Max(prevSwings8.Count, SwingCountPeak8);
            SwingCountPeak4 = Mathf.Max(prevSwings4.Count, SwingCountPeak4);

            NoteCountPeak16 = Mathf.Max(prevNotes16.Count, NoteCountPeak16);
            NoteCountPeak8 = Mathf.Max(prevNotes8.Count, NoteCountPeak8);
            NoteCountPeak4 = Mathf.Max(prevNotes4.Count, NoteCountPeak4);

            //The previous red/blue note becomes this note for next time
            if(isRed)
            {
                previousRedNote = currentNote;
            }
            else previousBlueNote = currentNote;
        }

        //Calculate actual SPS and NPS values for peaks
        SpsPeak16 = ((float)SwingCountPeak16 / (16 * beatTime)).Round(roundDigits);
        SpsPeak8 = ((float)SwingCountPeak8 / (8 * beatTime)).Round(roundDigits);
        SpsPeak4 = ((float)SwingCountPeak4 / (4 * beatTime)).Round(roundDigits);

        NpsPeak16 = ((float)NoteCountPeak16 / (16 * beatTime)).Round(roundDigits);
        NpsPeak8 = ((float)NoteCountPeak8 / (8 * beatTime)).Round(roundDigits);
        NpsPeak4 = ((float)NoteCountPeak4 / (4 * beatTime)).Round(roundDigits);

        OnStatsUpdated?.Invoke();
    }
}