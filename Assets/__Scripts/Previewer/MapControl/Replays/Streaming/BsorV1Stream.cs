using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class BsorV1Stream : ReplayStreamingSocket
{
    public Action<Replay> HandleNewReplay;
    public int PlayerID;

    public LevelEndType EndType { get; private set; }
    public float FailTime { get; private set; }
    public bool ShouldUpload { get; private set; }

    public Replay CurrentReplay;

    // Track state just as we would for a static replay
    private int frameCount => PlayerPositionManager.ReplayFrames.Count;

    private float lastCheckedFramerateTime = 0f;
    private int checkedFrameCount = 0;
    private int totalFPS = 0;
    private int averageFramerate = 0;


    public BsorV1Stream(Uri uri, int playerID, Action<Replay> OnNewReplay)
    {
        PlayerID = playerID;
        HandleNewReplay = OnNewReplay;
        ConnectAndStreamData(uri);
    }


    private void OnMapStarted()
    {
        StreamTime = 0f;
        HandleNewReplay?.Invoke(CurrentReplay);
    }


    private void ProcessIncrementalFrames(List<Frame> frames)
    {
        for(int i = 0; i < frames.Count; i++)
        {
            ReplayFrame newFrame = new ReplayFrame(frames[i]);
            //DeltaTime can be approximated based on framerate
            //(preferable in a stream because we don't have all the context)
            newFrame.DeltaTime = 1f / newFrame.FPS;

            //Calculate average framerates for displaying on the frame counter
            checkedFrameCount++;
            totalFPS += newFrame.FPS;

            if(i == 0)
            {
                averageFramerate = newFrame.FPS;
            }
            else
            {
                float timeDifference = newFrame.Time - lastCheckedFramerateTime;
                if(timeDifference >= FpsDisplay.FramerateSampleTime)
                {
                    averageFramerate = totalFPS / checkedFrameCount;

                    lastCheckedFramerateTime = newFrame.Time;
                    checkedFrameCount = 0;
                    totalFPS = 0;
                }
            }
            newFrame.AverageFPS = averageFramerate;

            if(newFrame.Time > StreamTime)
            {
                StreamTime = newFrame.Time;
            }

            PlayerPositionManager.ReplayFrames.Add(newFrame);
        }
    }


    private void ProcessIncrementalPlayerHeight(List<AutomaticHeight> heightEvents)
    {
        foreach(AutomaticHeight height in heightEvents)
        {
            ReplayManager.PlayerHeightEvents.Add(new PlayerHeightEvent(height));
        }
    }


    private void ProcessIncrementalNotes(List<NoteEvent> noteEvents)
    {
        ObjectManager objectManager = ObjectManager.Instance;

        foreach(NoteEvent noteEvent in noteEvents)
        {
            ScoringEvent scoringEvent = new ScoringEvent(noteEvent);
            float eventTime = scoringEvent.Time;

            List<Note> sameTimeNotes = objectManager.noteManager.Objects.GetObjectsAtTime(eventTime);
            Note noteMatch = sameTimeNotes.FirstOrDefault(x => x.ScoreEventID == noteEvent.noteID);
            if(noteMatch != null)
            {
                scoringEvent.SetEventValues(noteMatch.ScoringType, noteMatch.Position);
                continue;
            }

            List<Bomb> sameTimeBombs = objectManager.bombManager.Objects.GetObjectsAtTime(eventTime);
            Bomb bombMatch = sameTimeBombs.FirstOrDefault(x => x.ScoreEventID == noteEvent.noteID);
            if(bombMatch != null)
            {
                scoringEvent.SetEventValues(ScoringType.NoScore, bombMatch.Position);
                continue;
            }

            List<ChainLink> sameTimeLinks = objectManager.chainManager.Objects.GetObjectsAtTime(eventTime);
            ChainLink linkMatch = sameTimeLinks.FirstOrDefault(x => x.ScoreEventID == noteEvent.noteID);
            if(linkMatch != null)
            {
                scoringEvent.SetEventValues(ScoringType.ChainLink, linkMatch.Position);
                continue;
            }

            // Also check if this event is a chain link arc head
            int checkId = noteEvent.noteID - (int)ScoringType.ChainLinkArcHead * 10000;
            checkId += (int)ScoringType.ChainLink * 10000;
            linkMatch = sameTimeLinks.FirstOrDefault(x => x.ScoreEventID == checkId);
            if(linkMatch != null)
            {
                scoringEvent.SetEventValues(ScoringType.ChainLinkArcHead, linkMatch.Position);
                continue;
            }

            // Found no match, so use inference
            scoringEvent.InferEventValues();
        }
    }


    private void ProcessIncrementalWalls(List<WallEvent> wallEvents)
    {
        
    }


    public override void HandleMessage(byte[] message)
    {
        int pointer = 0;
        int length = message.Length;

        while(pointer < length)
        {
            int value = message[pointer++];
            try
            {
                if(value >= 0 && value <= (int)StructType.pauses)
                {
                    StructType type = (StructType)value;

                    switch(type)
                    {
                        case StructType.info:
                            CurrentReplay = new Replay
                            {
                                info = BsorDecoder.DecodeInfo(message, ref pointer)
                            };
                            // TODO: Figure out wtf is going on with score
                            // ResetScoreTracking();
                            OnMapStarted();
                            break;
                        case StructType.frames:
                            if(CurrentReplay != null)
                            {
                                List<Frame> newFrames = BsorDecoder.DecodeFrames(message, ref pointer);
                                CurrentReplay.frames.AddRange(newFrames);
                                ProcessIncrementalFrames(newFrames);
                            }
                            break;
                        case StructType.notes:
                            if(CurrentReplay != null)
                            {
                                List<NoteEvent> newNotes = BsorDecoder.DecodeNotes(message, ref pointer);
                                CurrentReplay.notes.AddRange(newNotes);
                                ProcessIncrementalNotes(newNotes);
                            }
                            break;
                        case StructType.walls:
                            if(CurrentReplay != null)
                            {
                                List<WallEvent> newWalls = BsorDecoder.DecodeWalls(message, ref pointer);
                                CurrentReplay.walls.AddRange(newWalls);
                                ProcessIncrementalWalls(newWalls);
                            }
                            break;
                        case StructType.heights:
                            if(CurrentReplay != null)
                            {
                                List<AutomaticHeight> newHeights = BsorDecoder.DecodeHeight(message, ref pointer);
                                CurrentReplay.heights.AddRange(newHeights);
                                ProcessIncrementalPlayerHeight(newHeights);
                            }
                            break;
                        case StructType.pauses:
                            if(CurrentReplay != null)
                            {
                                List<Pause> newPauses = BsorDecoder.DecodePauses(message, ref pointer);
                                CurrentReplay.pauses.AddRange(newPauses);
                                foreach (Pause pause in newPauses)
                                {
                                    // TODO: Handle pausing
                                }
                            }
                            break;
                    }
                }
                else if(value == 99)
                {
                    if(CurrentReplay != null)
                    {
                        CurrentReplay.info = BsorDecoder.DecodeInfo(message, ref pointer);
                    }
                    EndType = (LevelEndType)BsorDecoder.DecodeInt(message, ref pointer);
                    FailTime = BsorDecoder.DecodeFloat(message, ref pointer);
                    ShouldUpload = BsorDecoder.DecodeBool(message, ref pointer);

                    // TODO: Handle map end state
                }
            }
            catch(Exception e)
            {
                Debug.LogError($"Error parsing streamed replay chunk! {e.Message}, {e.StackTrace}"); 
            }
        }
    }


    public override async Task OnSocketConnect()
    {
        // A command to the server tells it what player to watch
        BLStreamCommand command = new BLStreamCommand()
        {
            action = "replay",
            playerId = PlayerID.ToString()
        };

        string json = JsonConvert.SerializeObject(command);
        await socket.SendAsync(Encoding.UTF8.GetBytes(json), WebSocketMessageType.Text, true, default);
    }


    private class BLStreamCommand
    {
        public string action;
        public string playerId;
    }


    public enum LevelEndType
    {
        Unknown = 0,
        Clear = 1,
        Fail = 2,
        Restart = 3,
        Quit = 4,
        Practice = 5
    }
}