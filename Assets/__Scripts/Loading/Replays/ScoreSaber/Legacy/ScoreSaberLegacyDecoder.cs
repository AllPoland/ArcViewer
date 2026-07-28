using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ArcViewer.LZMA;

public static class ScoreSaberLegacyDecoder
{
    public static Replay Decode(byte[] input)
    {
        try
        {
            return DecodeInternal(input);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to decode legacy ScoreSaber replay: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    private static Replay DecodeInternal(byte[] input)
    {
        byte[] decompressed = LzmaHelper.Decompress(input);
        List<LegacyKeyframe> keyframes = ParseNrbfKeyframes(decompressed);

        if(keyframes == null || keyframes.Count == 0)
        {
            Debug.LogWarning("Legacy ScoreSaber replay: no keyframes found!");
            return null;
        }

        return ConvertToReplay(keyframes);
    }

    private static Replay ConvertToReplay(List<LegacyKeyframe> keyframes)
    {
        Replay replay = new Replay
        {
            info = new ReplayInfo
            {
                version = "ScoreSaberLegacy",
                playerName = "ScoreSaber Player",
                score = keyframes[keyframes.Count - 1].score,
                gameVersion = "", timestamp = "", playerID = "", platform = "",
                trackingSytem = "", hmd = "", controller = "", hash = "",
                songName = "", mapper = "", difficulty = "", mode = "",
                environment = "", modifiers = ""
            },
            frames = new List<Frame>(keyframes.Count),
            scoreSaberLegacyScoreData = new List<LegacyScoreFrame>(keyframes.Count),
            notes = new List<NoteEvent>(),
            walls = new List<WallEvent>(),
            heights = new List<AutomaticHeight>(),
            pauses = new List<Pause>()
        };

        for(int i = 0; i < keyframes.Count; i++)
        {
            LegacyKeyframe kf = keyframes[i];
            Frame frame = new Frame
            {
                time = kf.time,
                fps = 0,
                rightHand = new PositionData { position = kf.pos1, rotation = kf.rot1 },
                leftHand = new PositionData { position = kf.pos2, rotation = kf.rot2 },
                head = new PositionData { position = kf.pos3, rotation = kf.rot3 }
            };

            if(frame.time != 0 && (replay.frames.Count == 0 || frame.time != replay.frames[replay.frames.Count - 1].time))
                replay.frames.Add(frame);

            replay.scoreSaberLegacyScoreData.Add(new LegacyScoreFrame
            {
                time = kf.time,
                score = kf.score,
                combo = kf.combo
            });
        }

        Debug.Log($"Legacy ScoreSaber replay decoded: {replay.frames.Count} frames, final score={replay.info.score}");
        return replay;
    }

    private struct LegacyKeyframe
    {
        public Vector3 pos1, pos2, pos3;
        public Quaternion rot1, rot2, rot3;
        public float time;
        public int combo, score;
    }

    private const byte NrbfSerializationHeader = 0;
    private const byte NrbfClassWithId = 1;
    private const byte NrbfClassWithMembersAndTypes = 5;
    private const byte NrbfBinaryArray = 7;
    private const byte NrbfMemberReference = 9;
    private const byte NrbfObjectNull = 10;
    private const byte NrbfMessageEnd = 11;
    private const byte NrbfBinaryLibrary = 12;
    private const byte NrbfArraySingleObject = 16;

    private static List<LegacyKeyframe> ParseNrbfKeyframes(byte[] data)
    {
        int offset = 0;
        List<LegacyKeyframe> keyframes = new List<LegacyKeyframe>();
        int keyframeClassId = -1;

        while(offset < data.Length)
        {
            byte recordType = data[offset++];

            switch(recordType)
            {
                case NrbfSerializationHeader:
                    offset += 16;
                    break;

                case NrbfBinaryLibrary:
                    offset += 4;
                    ReadNrbfString(data, ref offset);
                    break;

                case NrbfClassWithMembersAndTypes:
                {
                    int objectId = ScoreSaberUtils.ReadInt(data, ref offset);
                    string className = ReadNrbfString(data, ref offset);
                    int memberCount = ScoreSaberUtils.ReadInt(data, ref offset);

                    for(int i = 0; i < memberCount; i++)
                        ReadNrbfString(data, ref offset);

                    byte[] memberTypes = new byte[memberCount];
                    for(int i = 0; i < memberCount; i++)
                        memberTypes[i] = data[offset++];

                    for(int i = 0; i < memberCount; i++)
                        SkipNrbfTypeInfo(data, ref offset, memberTypes[i]);

                    offset += 4;

                    if(className.Contains("KeyframeSerializable"))
                    {
                        keyframeClassId = objectId;
                        if(memberCount == 24)
                            keyframes.Add(ReadLegacyKeyframeValues(data, ref offset));
                        else
                        {
                            Debug.LogWarning($"Unexpected KeyframeSerializable member count: {memberCount}");
                            offset += memberCount * 4;
                        }
                    }
                    break;
                }

                case NrbfClassWithId:
                {
                    ScoreSaberUtils.ReadInt(data, ref offset);
                    int metadataId = ScoreSaberUtils.ReadInt(data, ref offset);
                    if(metadataId == keyframeClassId)
                        keyframes.Add(ReadLegacyKeyframeValues(data, ref offset));
                    break;
                }

                case NrbfArraySingleObject:
                    offset += 8;
                    break;

                case NrbfBinaryArray:
                {
                    offset += 4;
                    byte arrayType = data[offset++];
                    int rank = ScoreSaberUtils.ReadInt(data, ref offset);
                    offset += rank * 4;
                    if(arrayType >= 3 && arrayType <= 5) offset += rank * 4;
                    byte binaryType = data[offset++];
                    SkipNrbfTypeInfo(data, ref offset, binaryType);
                    break;
                }

                case NrbfObjectNull:
                    break;

                case NrbfMemberReference:
                    offset += 4;
                    break;

                case NrbfMessageEnd:
                    return keyframes;

                default:
                    Debug.LogWarning($"Unknown NRBF record type: {recordType} at offset {offset - 1}");
                    return keyframes;
            }
        }

        return keyframes;
    }

    private static LegacyKeyframe ReadLegacyKeyframeValues(byte[] data, ref int offset)
    {
        return new LegacyKeyframe
        {
            pos1 = ScoreSaberUtils.ReadVector3(data, ref offset),
            pos2 = ScoreSaberUtils.ReadVector3(data, ref offset),
            pos3 = ScoreSaberUtils.ReadVector3(data, ref offset),
            rot1 = ReadQuaternion(data, ref offset),
            rot2 = ReadQuaternion(data, ref offset),
            rot3 = ReadQuaternion(data, ref offset),
            time = ScoreSaberUtils.ReadFloat(data, ref offset),
            combo = ScoreSaberUtils.ReadInt(data, ref offset),
            score = ScoreSaberUtils.ReadInt(data, ref offset)
        };
    }

    private static Quaternion ReadQuaternion(byte[] data, ref int offset)
    {
        return new Quaternion(
            ScoreSaberUtils.ReadFloat(data, ref offset), ScoreSaberUtils.ReadFloat(data, ref offset),
            ScoreSaberUtils.ReadFloat(data, ref offset), ScoreSaberUtils.ReadFloat(data, ref offset));
    }

    private static void SkipNrbfTypeInfo(byte[] data, ref int offset, byte binaryType)
    {
        switch(binaryType)
        {
            case 0: case 7: offset++; break;
            case 3: ReadNrbfString(data, ref offset); break;
            case 4: ReadNrbfString(data, ref offset); offset += 4; break;
        }
    }

    private static string ReadNrbfString(byte[] data, ref int offset)
    {
        int length = 0, shift = 0;
        byte b;
        do
        {
            b = data[offset++];
            length |= (b & 0x7F) << shift;
            shift += 7;
        } while((b & 0x80) != 0);

        string result = Encoding.UTF8.GetString(data, offset, length);
        offset += length;
        return result;
    }
}
