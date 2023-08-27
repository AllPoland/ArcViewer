using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Replay
{
    public ReplayInfo info = new ReplayInfo();

    public List<Frame> frames = new List<Frame>();

    public List<NoteEvent> notes = new List<NoteEvent>();
    public List<WallEvent> walls = new List<WallEvent>();
    public List<AutomaticHeight> heights = new List<AutomaticHeight>();
    public List<Pause> pauses = new List<Pause>();
}

public class ReplayInfo
{
    public string version;
    public string gameVersion;
    public string timestamp;
    
    public string playerID;
    public string playerName;
    public string platform;

    public string trackingSytem;
    public string hmd;
    public string controller;

    public string hash;
    public string songName;
    public string mapper;
    public string difficulty;

    public int score;
    public string mode;
    public string environment;
    public string modifiers;
    public float jumpDistance;
    public bool leftHanded;
    public float height;

    public float startTime;
    public float failTime;
    public float speed;
}

public class Frame
{
    public float time;
    public int fps;
    public PositionData head;
    public PositionData leftHand;
    public PositionData rightHand;
};

public enum NoteEventType
{
    good = 0,
    bad = 1,
    miss = 2,
    bomb = 3
}

public class NoteEvent
{
    public int noteID;
    public float eventTime;
    public float spawnTime;
    public NoteEventType eventType;
    public NoteCutInfo noteCutInfo;
};

public class WallEvent
{
    public int wallID;
    public float energy;
    public float time;
    public float spawnTime;
};

public class AutomaticHeight
{
    public float height;
    public float time;
};

public class Pause
{
    public long duration;
    public float time;
};

public class NoteCutInfo
{
    public bool speedOK;
    public bool directionOK;
    public bool saberTypeOK;
    public bool wasCutTooSoon;
    public float saberSpeed;
    public Vector3 saberDir;
    public int saberType;
    public float timeDeviation;
    public float cutDirDeviation;
    public Vector3 cutPoint;
    public Vector3 cutNormal;
    public float cutDistanceToCenter;
    public float cutAngle;
    public float beforeCutRating;
    public float afterCutRating;
};

public enum StructType
{
    info = 0,
    frames = 1,
    notes = 2,
    walls = 3,
    heights = 4,
    pauses = 5
}

public class PositionData
{
    public Vector3 position;
    public Quaternion rotation;
}

public class AsyncReplayDecoder
{
    public Replay replay = new Replay();

    int offset = 0;
    public byte[] replayData = new byte[1024000];

    private Stream stream;

    public async Task<(ReplayInfo, Task<Replay>)> StartDecodingStream(Stream stream)
    {
        int magic = await DecodeInt(stream);
        byte version = await DecodeByte(stream);

        if (magic == 0x442d3d69 && version == 1)
        {
            StructType type = (StructType)await DecodeByte(stream);
            if (type == StructType.info) {
                replay.info = await DecodeInfo(stream);
                this.stream = stream;
                return (replay.info, ContinueDecoding());
            } else {
                return (null, null);
            }
        }
        else
        {
            return (null, null);
        }
    }

    private async Task<Replay> ContinueDecoding() 
    {
        await Task.Yield();
        for (int a = (int)StructType.frames; a < ((int)StructType.pauses) + 1; a++) {
            StructType type = (StructType)await DecodeByte(stream);

            switch (type)
            {
                case StructType.frames:
                    replay.frames = await DecodeFrames(stream);
                    break;
                case StructType.notes:
                    replay.notes = await DecodeNotes(stream);
                    break;
                case StructType.walls:
                    replay.walls = await DecodeWalls(stream);
                    break;
                case StructType.heights:
                    replay.heights = await DecodeHeight(stream);
                    break;
                case StructType.pauses:
                    replay.pauses = await DecodePauses(stream);
                    break;
            }
        }

        Array.Resize(ref replayData, offset);

        return replay;
    }

    private async Task<ReplayInfo> DecodeInfo(Stream stream)
    {
            ReplayInfo result = new ReplayInfo();

            result.version = await DecodeString(stream);
            result.gameVersion = await DecodeString(stream);
            result.timestamp = await DecodeString(stream);

            result.playerID = await DecodeString(stream);
            result.playerName = await DecodeString(stream);
            result.platform = await DecodeString(stream);

            result.trackingSytem = await DecodeString(stream);
            result.hmd = await DecodeString(stream);
            result.controller = await DecodeString(stream);

            result.hash = await DecodeString(stream);
            result.songName = await DecodeString(stream);
            result.mapper = await DecodeString(stream);
            result.difficulty = await DecodeString(stream);
            
            result.score = await DecodeInt(stream);
            result.mode = await DecodeString(stream);
            result.environment = await DecodeString(stream);
            result.modifiers = await DecodeString(stream);
            result.jumpDistance = await DecodeFloat(stream);
            result.leftHanded = await DecodeBool(stream);
            result.height = await DecodeFloat(stream);

            result.startTime = await DecodeFloat(stream);
            result.failTime = await DecodeFloat(stream);
            result.speed = await DecodeFloat(stream);

            return result;
    }

    private async Task<List<Frame>> DecodeFrames(Stream stream)
    {
        int length = await DecodeInt(stream);
        List<Frame> result = new List<Frame>();
        for (int i = 0; i < length; i++)
        {
            Frame frame = await DecodeFrame(stream);
            if (frame.time != 0 && (result.Count == 0 || frame.time != result[result.Count - 1].time))
            {
                result.Add(frame);
            }
        }
        return result;
    }

    private async Task<Frame> DecodeFrame(Stream stream)
    {
        Frame result = new Frame();
        result.time = await DecodeFloat(stream);
        result.fps = await DecodeInt(stream);
        result.head = await DecodeEuler(stream);
        result.leftHand = await DecodeEuler(stream);
        result.rightHand = await DecodeEuler(stream);

        return result;
    }

    private async Task<List<NoteEvent>> DecodeNotes(Stream stream)
    {
        int length = await DecodeInt(stream);
        List<NoteEvent> result = new List<NoteEvent>();
        for (int i = 0; i < length; i++)
        {
            result.Add(await DecodeNote(stream));
        }
        return result;
    }

    private async Task<List<WallEvent>> DecodeWalls(Stream stream)
    {
        int length = await DecodeInt(stream);
        List<WallEvent> result = new List<WallEvent>();
        for (int i = 0; i < length; i++)
        {
            WallEvent wall = new WallEvent();
            wall.wallID = await DecodeInt(stream);
            wall.energy = await DecodeFloat(stream);
            wall.time = await DecodeFloat(stream);
            wall.spawnTime = await DecodeFloat(stream);
            result.Add(wall);
        }
        return result;
    }

    private async Task<List<AutomaticHeight>> DecodeHeight(Stream stream)
    {
        int length = await DecodeInt(stream);
        List<AutomaticHeight> result = new List<AutomaticHeight>();
        for (int i = 0; i < length; i++)
        {
            AutomaticHeight height = new AutomaticHeight();
            height.height = await DecodeFloat(stream);
            height.time = await DecodeFloat(stream);
            result.Add(height);
        }
        return result;
    }

    private async Task<List<Pause>> DecodePauses(Stream stream)
    {
        int length = await DecodeInt(stream);
        List<Pause> result = new List<Pause>();
        for (int i = 0; i < length; i++)
        {
            Pause pause = new Pause();
            pause.duration = await DecodeLong(stream);
            pause.time = await DecodeFloat(stream);
            result.Add(pause);
        }
        return result;
    }

    private async Task<NoteEvent> DecodeNote(Stream stream)
    {
        NoteEvent result = new NoteEvent();
        result.noteID = await DecodeInt(stream);
        result.eventTime = await DecodeFloat(stream);
        result.spawnTime = await DecodeFloat(stream);
        result.eventType = (NoteEventType) await DecodeInt(stream);
        if (result.eventType == NoteEventType.good || result.eventType == NoteEventType.bad) {
            result.noteCutInfo = await DecodeCutInfo(stream);
        }

        if (result.noteID == -1 || (result.noteID > 0 && result.noteID < 100000 && result.noteID % 10 == 9)) {
            result.noteID += 4;
            result.eventType = NoteEventType.bomb;
        }

        return result;
    }

    private async Task<NoteCutInfo> DecodeCutInfo(Stream stream)
    {
        NoteCutInfo result = new NoteCutInfo();
        result.speedOK = await DecodeBool(stream);
        result.directionOK = await DecodeBool(stream);
        result.saberTypeOK = await DecodeBool(stream);
        result.wasCutTooSoon = await DecodeBool(stream);
        result.saberSpeed = await DecodeFloat(stream);
        result.saberDir = await DecodeVector3(stream);
        result.saberType = await DecodeInt(stream);
        result.timeDeviation = await DecodeFloat(stream);
        result.cutDirDeviation = await DecodeFloat(stream);
        result.cutPoint = await DecodeVector3(stream);
        result.cutNormal = await DecodeVector3(stream);
        result.cutDistanceToCenter = await DecodeFloat(stream);
        result.cutAngle = await DecodeFloat(stream);
        result.beforeCutRating = await DecodeFloat(stream);
        result.afterCutRating = await DecodeFloat(stream);
        return result;
    }

    private async Task<PositionData> DecodeEuler(Stream stream)
    {
        PositionData result = new PositionData();
        result.position = await DecodeVector3(stream);
        result.rotation = await DecodeQuaternion(stream);

        return result;
    }

    private async Task<Vector3> DecodeVector3(Stream stream)
    {
        Vector3 result = new Vector3();
        result.x = await DecodeFloat(stream);
        result.y = await DecodeFloat(stream);
        result.z = await DecodeFloat(stream);

        return result;
    }

    private async Task<Quaternion> DecodeQuaternion(Stream stream)
    {
        Quaternion result = new Quaternion();
        result.x = await DecodeFloat(stream);
        result.y = await DecodeFloat(stream);
        result.z = await DecodeFloat(stream);
        result.w = await DecodeFloat(stream);

        return result;
    }

    private void EnsureBufferSize(int size) {
        if (offset + size > replayData.Length) {
            Array.Resize(ref replayData, replayData.Length * 2);
        }
    }

    private async Task<long> DecodeLong(Stream stream)
    {
        EnsureBufferSize(8);
        await stream.ReadAsync(replayData, offset, 8);
        offset += 8;
        return BitConverter.ToInt64(replayData, offset - 8);
    }

    private async Task<int> DecodeInt(Stream stream)
    {
        EnsureBufferSize(4);
        await stream.ReadAsync(replayData, offset, 4);
        offset += 4;
        return BitConverter.ToInt32(replayData, offset - 4);
    }

    private async Task<byte> DecodeByte(Stream stream)
    {
        EnsureBufferSize(1);
        await stream.ReadAsync(replayData, offset, 1);
        offset++;
        return replayData[offset - 1];
    }

    private async Task<string> DecodeString(Stream stream, int size = 4)
    {
        EnsureBufferSize(size);
        await stream.ReadAsync(replayData, offset, size);
        offset += size;
        int length = BitConverter.ToInt32(replayData, offset - 4);

        if (length > 1000 || length < 0)
        {
            return await DecodeString(stream, 1);
        }

        EnsureBufferSize(length);
        await stream.ReadAsync(replayData, offset, length);
        string @string = Encoding.UTF8.GetString(replayData, offset, length);
        offset += length;
        return @string;
    }

    private async Task<float> DecodeFloat(Stream stream)
    {
        EnsureBufferSize(4);
        await stream.ReadAsync(replayData, offset, 4);
        offset += 4;
        return BitConverter.ToSingle(replayData, offset - 4);
    }

    private async Task<bool> DecodeBool(Stream stream)
    {
        EnsureBufferSize(1);
        await stream.ReadAsync(replayData, offset, 1);
        offset++;
        return BitConverter.ToBoolean(replayData, offset - 1);
    }
}

public static class ReplayDecoder
{
    public static Replay Decode(byte[] buffer)
    {
        int arrayLength = (int)buffer.Length;

        int pointer = 0;

        int magic = DecodeInt(buffer, ref pointer);
        byte version = buffer[pointer++];

        if (magic == 0x442d3d69 && version == 1)
        {
            Replay replay = new Replay();

            for (int a = 0; a < ((int)StructType.pauses) + 1 && pointer < arrayLength; a++)
            {
                StructType type = (StructType)buffer[pointer++];

                switch (type)
                {
                    case StructType.info:
                        replay.info = DecodeInfo(buffer, ref pointer);
                        break;
                    case StructType.frames:
                        replay.frames = DecodeFrames(buffer, ref pointer);
                        break;
                    case StructType.notes:
                        replay.notes = DecodeNotes(buffer, ref pointer);
                        break;
                    case StructType.walls:
                        replay.walls = DecodeWalls(buffer, ref pointer);
                        break;
                    case StructType.heights:
                        replay.heights = DecodeHeight(buffer, ref pointer);
                        break;
                    case StructType.pauses:
                        replay.pauses = DecodePauses(buffer, ref pointer);
                        break;
                    }
            }

            return replay;
        }
        else
        {
            return null;
        }
    }

    private static ReplayInfo DecodeInfo(byte[] buffer, ref int pointer)
    {
            ReplayInfo result = new ReplayInfo();

            result.version = DecodeString(buffer, ref pointer);
            result.gameVersion = DecodeString(buffer, ref pointer);
            result.timestamp = DecodeString(buffer, ref pointer);

            result.playerID = DecodeString(buffer, ref pointer);
            result.playerName = DecodeName(buffer, ref pointer);
            result.platform = DecodeString(buffer, ref pointer);

            result.trackingSytem = DecodeString(buffer, ref pointer);
            result.hmd = DecodeString(buffer, ref pointer);
            result.controller = DecodeString(buffer, ref pointer);

            result.hash = DecodeString(buffer, ref pointer);
            result.songName = DecodeString(buffer, ref pointer);
            result.mapper = DecodeString(buffer, ref pointer);
            result.difficulty = DecodeString(buffer, ref pointer);
            
            result.score = DecodeInt(buffer, ref pointer);
            result.mode = DecodeString(buffer, ref pointer);
            result.environment = DecodeString(buffer, ref pointer);
            result.modifiers = DecodeString(buffer, ref pointer);
            result.jumpDistance = DecodeFloat(buffer, ref pointer);
            result.leftHanded = DecodeBool(buffer, ref pointer);
            result.height = DecodeFloat(buffer, ref pointer);

            result.startTime = DecodeFloat(buffer, ref pointer);
            result.failTime = DecodeFloat(buffer, ref pointer);
            result.speed = DecodeFloat(buffer, ref pointer);

            return result;
        }

    private static List<Frame> DecodeFrames(byte[] buffer, ref int pointer)
    {
        int length = DecodeInt(buffer, ref pointer);
        List<Frame> result = new List<Frame>();
        for (int i = 0; i < length; i++)
        {
            var frame  = DecodeFrame(buffer, ref pointer);
            if (frame.time != 0 && (result.Count == 0 || frame.time != result[result.Count - 1].time)) {
                result.Add(frame);
            }
        }
        return result;
    }

    private static Frame DecodeFrame(byte[] buffer, ref int pointer)
    {
        Frame result = new Frame();
        result.time = DecodeFloat(buffer, ref pointer);
        result.fps = DecodeInt(buffer, ref pointer);
        result.head = DecodeEuler(buffer, ref pointer);
        result.leftHand = DecodeEuler(buffer, ref pointer);
        result.rightHand = DecodeEuler(buffer, ref pointer);

        return result;
    }

    private static List<NoteEvent> DecodeNotes(byte[] buffer, ref int pointer)
    {
        int length = DecodeInt(buffer, ref pointer);
        List<NoteEvent> result = new List<NoteEvent>();
        for (int i = 0; i < length; i++)
        {
            result.Add(DecodeNote(buffer, ref pointer));
        }
        return result;
    }

    private static List<WallEvent> DecodeWalls(byte[] buffer, ref int pointer)
    {
        int length = DecodeInt(buffer, ref pointer);
        List<WallEvent> result = new List<WallEvent>();
        for (int i = 0; i < length; i++)
        {
            WallEvent wall = new WallEvent();
            wall.wallID = DecodeInt(buffer, ref pointer);
            wall.energy = DecodeFloat(buffer, ref pointer);
            wall.time = DecodeFloat(buffer, ref pointer);
            wall.spawnTime = DecodeFloat(buffer, ref pointer);
            result.Add(wall);
        }
        return result;
    }

    private static List<AutomaticHeight> DecodeHeight(byte[] buffer, ref int pointer)
    {
        int length = DecodeInt(buffer, ref pointer);
        List<AutomaticHeight> result = new List<AutomaticHeight>();
        for (int i = 0; i < length; i++)
        {
            AutomaticHeight height = new AutomaticHeight();
            height.height = DecodeFloat(buffer, ref pointer);
            height.time = DecodeFloat(buffer, ref pointer);
            result.Add(height);
        }
        return result;
    }

    private static List<Pause> DecodePauses(byte[] buffer, ref int pointer)
    {
        int length = DecodeInt(buffer, ref pointer);
        List<Pause> result = new List<Pause>();
        for (int i = 0; i < length; i++)
        {
            Pause pause = new Pause();
            pause.duration = DecodeLong(buffer, ref pointer);
            pause.time = DecodeFloat(buffer, ref pointer);
            result.Add(pause);
        }
        return result;
    }

    private static NoteEvent DecodeNote(byte[] buffer, ref int pointer)
    {
        NoteEvent result = new NoteEvent();
        result.noteID = DecodeInt(buffer, ref pointer);
        result.eventTime = DecodeFloat(buffer, ref pointer);
        result.spawnTime = DecodeFloat(buffer, ref pointer);
        result.eventType = (NoteEventType)DecodeInt(buffer, ref pointer);
        if (result.eventType == NoteEventType.good || result.eventType == NoteEventType.bad) {
            result.noteCutInfo = DecodeCutInfo(buffer, ref pointer);
        }

        if (result.noteID == -1 || (result.noteID > 0 && result.noteID < 100000 && result.noteID % 10 == 9)) {
            result.noteID += 4;
            result.eventType = NoteEventType.bomb;
        }

        return result;
    }

    private static NoteCutInfo DecodeCutInfo(byte[] buffer, ref int pointer)
    {
        NoteCutInfo result = new NoteCutInfo();
        result.speedOK = DecodeBool(buffer, ref pointer);
        result.directionOK = DecodeBool(buffer, ref pointer);
        result.saberTypeOK = DecodeBool(buffer, ref pointer);
        result.wasCutTooSoon = DecodeBool(buffer, ref pointer);
        result.saberSpeed = DecodeFloat(buffer, ref pointer);
        result.saberDir = DecodeVector3(buffer, ref pointer);
        result.saberType = DecodeInt(buffer, ref pointer);
        result.timeDeviation = DecodeFloat(buffer, ref pointer);
        result.cutDirDeviation = DecodeFloat(buffer, ref pointer);
        result.cutPoint = DecodeVector3(buffer, ref pointer);
        result.cutNormal = DecodeVector3(buffer, ref pointer);
        result.cutDistanceToCenter = DecodeFloat(buffer, ref pointer);
        result.cutAngle = DecodeFloat(buffer, ref pointer);
        result.beforeCutRating = DecodeFloat(buffer, ref pointer);
        result.afterCutRating = DecodeFloat(buffer, ref pointer);
        return result;
    }

    private static PositionData DecodeEuler(byte[] buffer, ref int pointer)
    {
        PositionData result = new PositionData();
        result.position = DecodeVector3(buffer, ref pointer);
        result.rotation = DecodeQuaternion(buffer, ref pointer);

        return result;
    }

    private static Vector3 DecodeVector3(byte[] buffer, ref int pointer)
    {
        Vector3 result = new Vector3();
        result.x = DecodeFloat(buffer, ref pointer);
        result.y = DecodeFloat(buffer, ref pointer);
        result.z = DecodeFloat(buffer, ref pointer);

        return result;
    }

    private static Quaternion DecodeQuaternion(byte[] buffer, ref int pointer)
    {
        Quaternion result = new Quaternion();
        result.x = DecodeFloat(buffer, ref pointer);
        result.y = DecodeFloat(buffer, ref pointer);
        result.z = DecodeFloat(buffer, ref pointer);
        result.w = DecodeFloat(buffer, ref pointer);

        return result;
    }

    private static long DecodeLong(byte[] buffer, ref int pointer)
    {
        long result = BitConverter.ToInt64(buffer, pointer);
        pointer += 8;
        return result;
    }

    private static int DecodeInt(byte[] buffer, ref int pointer)
    {
        int result = BitConverter.ToInt32(buffer, pointer);
        pointer += 4;
        return result;
    }

    private static string DecodeName(byte[] buffer, ref int pointer)
    {
        int length = BitConverter.ToInt32(buffer, pointer);
        int lengthOffset = 0;
        if (length > 0)
        {
            while (BitConverter.ToInt32(buffer, length + pointer + 4 + lengthOffset) != 6 
                && BitConverter.ToInt32(buffer, length + pointer + 4 + lengthOffset) != 5 
                && BitConverter.ToInt32(buffer, length + pointer + 4 + lengthOffset) != 8)
            {
                lengthOffset++;
            }
        }
        string @string = Encoding.UTF8.GetString(buffer, pointer + 4, length + lengthOffset);
        pointer += length + 4 + lengthOffset;
        return @string;
    }

    private static string DecodeString(byte[] buffer, ref int pointer)
    {
        int length = BitConverter.ToInt32(buffer, pointer);
        if (length > 300 || length < 0)
        {
            pointer += 1;
            return DecodeString(buffer, ref pointer);
        }
        string @string = Encoding.UTF8.GetString(buffer, pointer + 4, length);
        pointer += length + 4;
        return @string;
    }

    private static float DecodeFloat(byte[] buffer, ref int pointer)
    {
        float result = BitConverter.ToSingle(buffer, pointer);
        pointer += 4;
        return result;
    }

    private static bool DecodeBool(byte[] buffer, ref int pointer)
    {
        bool result = BitConverter.ToBoolean(buffer, pointer);
        pointer++;
        return result;
    }
}