using System;
using System.Collections.Generic;
using UnityEngine;
using ArcViewer.LZMA;

public static class ScoreSaberDecoder
{
    public static bool IsScoreSaberReplay(byte[] data) =>
        data != null && ScoreSaberUtils.HasMagicHeader(data, data.Length);

    public static bool IsLegacyScoreSaberReplay(byte[] data) =>
        data != null && ScoreSaberUtils.HasLegacyHeader(data, data.Length);

    public static bool IsScoreSaberFile(string filePath)
    {
        try
        {
            byte[] header = new byte[ScoreSaberUtils.MagicLength];
            using(System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                int bytesRead = fs.Read(header, 0, ScoreSaberUtils.MagicLength);
                return ScoreSaberUtils.HasMagicHeader(header, bytesRead) || ScoreSaberUtils.HasLegacyHeader(header, bytesRead);
            }
        }
        catch
        {
            return false;
        }
    }

    public static Replay Decode(byte[] input)
    {
        if(!IsScoreSaberReplay(input)) return null;
        try
        {
            return DecodeInternal(input);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to decode ScoreSaber replay: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    private static Replay DecodeInternal(byte[] input)
    {
        byte[] compressed = new byte[input.Length - ScoreSaberUtils.MagicLength];
        Array.Copy(input, ScoreSaberUtils.MagicLength, compressed, 0, compressed.Length);
        byte[] data = LzmaHelper.Decompress(compressed);

        int offset = 0;
        int metadataPtr = ScoreSaberUtils.ReadInt(data, ref offset);
        int posePtr = ScoreSaberUtils.ReadInt(data, ref offset);
        int heightPtr = ScoreSaberUtils.ReadInt(data, ref offset);
        int notePtr = ScoreSaberUtils.ReadInt(data, ref offset);
        int scorePtr = ScoreSaberUtils.ReadInt(data, ref offset);
        offset += 16; // comboPtr, multiplierPtr, energyPtr, fpsPtr

        (ReplayInfo info, string version) = ReadMetadata(data, ref metadataPtr);
        bool isV3 = ScoreSaberUtils.VersionAtLeast(version, 3, 0, 0);

        List<Frame> frames = ReadFrames(data, ref posePtr);
        List<AutomaticHeight> heights = ReadHeightEvents(data, ref heightPtr);

        int noteCount = ScoreSaberUtils.ReadInt(data, ref notePtr);
        List<NoteEvent> notes = new List<NoteEvent>(noteCount);
        for(int i = 0; i < noteCount; i++)
            notes.Add(ReadNoteEvent(data, ref notePtr, isV3));

        info.score = ReadFinalScore(data, ref scorePtr, isV3);

        Debug.Log($"ScoreSaber replay decoded: {notes.Count} notes, {frames.Count} frames, score={info.score}");

        return new Replay
        {
            info = info,
            frames = frames,
            notes = notes,
            heights = heights,
            walls = new List<WallEvent>(),
            pauses = new List<Pause>()
        };
    }

    private static (ReplayInfo info, string version) ReadMetadata(byte[] data, ref int offset)
    {
        string version = ScoreSaberUtils.ReadString(data, ref offset);
        if(string.IsNullOrEmpty(version)) version = "2.0.0";

        string levelID = ScoreSaberUtils.ReadString(data, ref offset);
        int difficulty = ScoreSaberUtils.ReadInt(data, ref offset);
        string characteristic = ScoreSaberUtils.ReadString(data, ref offset);
        string environment = ScoreSaberUtils.ReadString(data, ref offset);
        string[] modifiers = ScoreSaberUtils.ReadStringArray(data, ref offset);
        ScoreSaberUtils.ReadFloat(data, ref offset); // NoteSpawnOffset
        bool leftHanded = ScoreSaberUtils.ReadBool(data, ref offset);
        float initialHeight = ScoreSaberUtils.ReadFloat(data, ref offset);
        offset += 16; // RoomRotation + RoomCenter
        float failTime = ScoreSaberUtils.ReadFloat(data, ref offset);

        if(ScoreSaberUtils.VersionAtLeast(version, 3, 1, 0))
        {
            ScoreSaberUtils.ReadString(data, ref offset);
            ScoreSaberUtils.ReadString(data, ref offset);
            ScoreSaberUtils.ReadString(data, ref offset);
        }

        string hash = "";
        if(!string.IsNullOrEmpty(levelID))
        {
            const string prefix = "custom_level_";
            hash = levelID.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase)
                ? levelID.Substring(prefix.Length) : levelID;
        }

        ReplayInfo info = new ReplayInfo
        {
            version = "ScoreSaber",
            playerName = "ScoreSaber Player",
            hash = hash,
            difficulty = ReplayInfo.DifficultyNameFromRaw(difficulty),
            mode = characteristic ?? "Standard",
            environment = environment ?? "",
            modifiers = modifiers != null ? string.Join(",", modifiers) : "",
            leftHanded = leftHanded,
            height = initialHeight,
            failTime = failTime,
            gameVersion = "", timestamp = "", playerID = "", platform = "",
            trackingSytem = "", hmd = "", controller = "", songName = "", mapper = ""
        };

        return (info, version);
    }

    private static List<Frame> ReadFrames(byte[] data, ref int offset)
    {
        int count = ScoreSaberUtils.ReadInt(data, ref offset);
        List<Frame> frames = new List<Frame>(count);
        for(int i = 0; i < count; i++)
        {
            Frame frame = new Frame
            {
                head = ScoreSaberUtils.ReadPositionData(data, ref offset),
                leftHand = ScoreSaberUtils.ReadPositionData(data, ref offset),
                rightHand = ScoreSaberUtils.ReadPositionData(data, ref offset),
                fps = ScoreSaberUtils.ReadInt(data, ref offset),
                time = ScoreSaberUtils.ReadFloat(data, ref offset)
            };

            if(frame.time != 0 && (frames.Count == 0 || frame.time != frames[frames.Count - 1].time))
                frames.Add(frame);
        }
        return frames;
    }

    private static NoteEvent ReadNoteEvent(byte[] data, ref int offset, bool v3)
    {
        float noteTime = ScoreSaberUtils.ReadFloat(data, ref offset);
        int lineLayer = ScoreSaberUtils.ReadInt(data, ref offset);
        int lineIndex = ScoreSaberUtils.ReadInt(data, ref offset);
        int colorType = ScoreSaberUtils.ReadInt(data, ref offset);
        int cutDirection = ScoreSaberUtils.ReadInt(data, ref offset);

        int scoringType = -1;
        if(v3)
        {
            ScoreSaberUtils.ReadInt(data, ref offset); // gameplayType
            scoringType = ScoreSaberUtils.ReadInt(data, ref offset);
            ScoreSaberUtils.ReadFloat(data, ref offset); // cutDirAngleOffset
        }

        int ssEventType = ScoreSaberUtils.ReadInt(data, ref offset);

        Vector3 cutPoint = ScoreSaberUtils.ReadVector3(data, ref offset);
        Vector3 cutNormal = ScoreSaberUtils.ReadVector3(data, ref offset);
        Vector3 saberDirection = ScoreSaberUtils.ReadVector3(data, ref offset);
        int saberType = ScoreSaberUtils.ReadInt(data, ref offset);
        bool directionOK = ScoreSaberUtils.ReadBool(data, ref offset);
        float saberSpeed = ScoreSaberUtils.ReadFloat(data, ref offset);
        float cutAngle = ScoreSaberUtils.ReadFloat(data, ref offset);
        float cutDistanceToCenter = ScoreSaberUtils.ReadFloat(data, ref offset);
        float cutDirectionDeviation = ScoreSaberUtils.ReadFloat(data, ref offset);
        float beforeCutRating = ScoreSaberUtils.ReadFloat(data, ref offset);
        float afterCutRating = ScoreSaberUtils.ReadFloat(data, ref offset);
        float eventTime = ScoreSaberUtils.ReadFloat(data, ref offset);
        offset += 8; // unityTimescale + timeSyncTimescale

        if(v3) offset += 64; // TimeDeviation + WorldRotation + InverseWorldRotation + NoteRotation + NotePosition

        NoteEventType eventType = ssEventType switch
        {
            1 => NoteEventType.good,
            2 => NoteEventType.bad,
            3 => NoteEventType.miss,
            4 => NoteEventType.bomb,
            _ => NoteEventType.miss
        };

        NoteEvent noteEvent = new NoteEvent
        {
            spawnTime = noteTime,
            eventTime = eventTime,
            eventType = eventType,
            lineIndex = lineIndex,
            lineLayer = lineLayer,
            colorType = colorType,
            cutDirection = cutDirection
        };

        if(eventType == NoteEventType.bomb)
            noteEvent.noteScoringType = (int)ScoringType.NoScore;
        else if(v3 && scoringType >= 0)
            noteEvent.noteScoringType = MapScoringType(scoringType);
        else
            noteEvent.noteScoringType = (int)ScoringType.Note;

        if(eventType == NoteEventType.good || eventType == NoteEventType.bad)
        {
            noteEvent.noteCutInfo = new NoteCutInfo
            {
                speedOK = saberSpeed > 2f,
                directionOK = directionOK,
                saberTypeOK = true,
                saberSpeed = saberSpeed,
                saberDir = saberDirection,
                saberType = saberType,
                cutDirDeviation = cutDirectionDeviation,
                cutPoint = cutPoint,
                cutNormal = cutNormal,
                cutDistanceToCenter = cutDistanceToCenter,
                cutAngle = cutAngle,
                beforeCutRating = beforeCutRating,
                afterCutRating = afterCutRating
            };
        }

        return noteEvent;
    }

    private static int MapScoringType(int ssScoringType) => ssScoringType switch
    {
        -1 => (int)ScoringType.Ignore,
        0 => (int)ScoringType.NoScore,
        1 => (int)ScoringType.Note,
        2 => (int)ScoringType.ArcHead,
        3 => (int)ScoringType.ArcTail,
        4 => (int)ScoringType.ChainHead,
        5 => (int)ScoringType.ChainLink,
        6 => (int)ScoringType.ArcHeadArcTail,
        7 => (int)ScoringType.ChainHeadArcTail,
        8 => (int)ScoringType.ChainLinkArcHead,
        9 => (int)ScoringType.ChainHeadArcHead,
        10 => (int)ScoringType.ChainHeadArcHeadArcTail,
        _ => (int)ScoringType.Note
    };

    private static int ReadFinalScore(byte[] data, ref int offset, bool isV3)
    {
        int count = ScoreSaberUtils.ReadInt(data, ref offset);
        int lastScore = 0;
        for(int i = 0; i < count; i++)
        {
            lastScore = ScoreSaberUtils.ReadInt(data, ref offset);
            ScoreSaberUtils.ReadFloat(data, ref offset);
            if(isV3) ScoreSaberUtils.ReadInt(data, ref offset);
        }
        return lastScore;
    }

    private static List<AutomaticHeight> ReadHeightEvents(byte[] data, ref int offset)
    {
        int count = ScoreSaberUtils.ReadInt(data, ref offset);
        List<AutomaticHeight> events = new List<AutomaticHeight>(count);
        for(int i = 0; i < count; i++)
        {
            events.Add(new AutomaticHeight
            {
                height = ScoreSaberUtils.ReadFloat(data, ref offset),
                time = ScoreSaberUtils.ReadFloat(data, ref offset)
            });
        }
        return events;
    }
}
