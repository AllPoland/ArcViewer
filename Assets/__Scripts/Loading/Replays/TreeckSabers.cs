using System;
using System.Collections.Generic;
using UnityEngine;

namespace TreeckSabers
{
    public class TricksReplay
    {
        public int Version;
        public TricksHandReplay LeftSaber = new TricksHandReplay();
        public TricksHandReplay RightSaber = new TricksHandReplay();
    }


    public class TricksHandReplay
    {
        public TricksSegment[] Segments = new TricksSegment[0];
    }


    public class TricksSegment
    {
        public TricksFrame[] Frames = new TricksFrame[0];
    }


    public class TricksFrame
    {
        public float SongTime;
        public ReeTransform SaberPos = new ReeTransform();
    }


    public class ReeTransform
    {
        public Vector3 Position;
        public Quaternion Rotation;
    }


    public class TreeckSaberDecoder
    {
        private const string key = "reesabers:tricks-replay";
        private const int expectedMagic = 1630166513;


        private static int FindFrameIndexByTime(IReadOnlyList<Frame> frames, float songTime, int startFrom = 0)
        {
            var index = startFrom;

            if(songTime >= frames[index].time)
            {
                //Search forwards
                for (var i = index; i < frames.Count; ++i)
                {
                    if (songTime < frames[i].time) break;
                    index = i;
                }

                return index;
            }

            //Search backwards
            for(int i = index; i >= 0; --i)
            {
                if (songTime > frames[i].time) break;
                index = i;
            }

            return index;
        }


        private static void ApplyPose(PositionData saberTransform, ReeTransform trickTransform)
        {
            saberTransform.position = trickTransform.Position;
            saberTransform.rotation = trickTransform.Rotation;
        }


        private static void ApplyTricks(ref List<Frame> frames, TricksReplay tricksReplay) {
            int frameIndex = 0;

            foreach(TricksSegment segment in tricksReplay.LeftSaber.Segments)
            {
                foreach(TricksFrame frame in segment.Frames)
                {
                    frameIndex = FindFrameIndexByTime(frames, frame.SongTime, frameIndex);
                    ApplyPose(frames[frameIndex].leftHand, frame.SaberPos);
                }
            }

            frameIndex = 0;

            foreach(TricksSegment segment in tricksReplay.RightSaber.Segments)
            {
                foreach(TricksFrame frame in segment.Frames)
                {
                    frameIndex = FindFrameIndexByTime(frames, frame.SongTime, frameIndex);
                    ApplyPose(frames[frameIndex].rightHand, frame.SaberPos);
                }
            }
        }


        private static TricksHandReplay DecodeHandReplay(byte[] buffer, ref int pointer)
        {
            TricksHandReplay handReplay = new TricksHandReplay();

            int segmentCount = ReplayDecoder.DecodeInt(buffer, ref pointer);
            handReplay.Segments = new TricksSegment[segmentCount];

            for(int s = 0; s < segmentCount; s++)
            {
                TricksSegment segment = new TricksSegment();

                int frameCount = ReplayDecoder.DecodeInt(buffer, ref pointer);
                segment.Frames = new TricksFrame[frameCount];

                for(int f = 0; f < frameCount; f++)
                {
                    segment.Frames[f] = new TricksFrame
                    {
                        SongTime = ReplayDecoder.DecodeFloat(buffer, ref pointer),
                        SaberPos = new ReeTransform
                        {
                            Position = ReplayDecoder.DecodeVector3(buffer, ref pointer),
                            Rotation = ReplayDecoder.DecodeQuaternion(buffer, ref pointer)
                        }
                    };
                }

                handReplay.Segments[s] = segment;
            }

            return handReplay;
        }


        private static void DecodeTricksReplay(byte[] buffer, ref int pointer, ref TricksReplay replay)
        {
            replay.Version = ReplayDecoder.DecodeInt(buffer, ref pointer);
            replay.LeftSaber = DecodeHandReplay(buffer, ref pointer);
            replay.RightSaber = DecodeHandReplay(buffer, ref pointer);
        }


        public static void ProcessReplay(Replay replay)
        {
            if(!replay.customData.ContainsKey(key))
            {
                //This replay doesn't have TreeckSabers custom data
                return;
            }

            Debug.Log("Replay contains TreeckSaber data.");

            //Decode the tricks data
            byte[] buffer = replay.customData[key];
            int pointer = 0;

            int magic = ReplayDecoder.DecodeInt(buffer, ref pointer);
            if(magic != expectedMagic)
            {
                //This data does not include the magic number and is invalid
                Debug.LogWarning("Custom TreeckSaber data is invalid!");
                return;
            }

            TricksReplay tricksReplay = new TricksReplay();
            try
            {
                DecodeTricksReplay(buffer, ref pointer, ref tricksReplay);
            }
            catch(Exception err)
            {
                //Decoding the tricks failed, don't modify the original replay
                Debug.LogWarning($"Failed to decode TreeckSaber data with error: {err.Message}, {err.StackTrace}");
                return;
            }

            //Apply the trick frames to the main replay
            ApplyTricks(ref replay.frames, tricksReplay);
        }
    }
}