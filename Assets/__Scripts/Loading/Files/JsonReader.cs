using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class JsonReader
{
    public static async Task<BeatmapInfo> LoadInfoAsync(string location)
    {
        Debug.Log("Loading text from Info.dat");
        string json = await ReadFileAsync(location);

        Debug.Log("Parsing Info.dat.");
        BeatmapInfo info = ParseInfoFromJson(json);

        return info;
    }


    public static BeatmapInfo ParseInfoFromJson(string json)
    {
        BeatmapInfo info = new BeatmapInfo();

        try
        {
            info = JsonUtility.FromJson<BeatmapInfo>(json);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unable to parse info from json with error: {err.Message}, {err.StackTrace}.");
            info = new BeatmapInfo();
        }

        if(info != null)
        {
            info = BeatmapUtility.AddNullsInfo(info);
        }
        return info;
    }


    public static async Task<Difficulty> LoadDifficultyAsync(string directory, DifficultyBeatmap beatmap)
    {
        Difficulty output = new Difficulty
        {
            difficultyRank = BeatmapLoader.DiffValueFromString[beatmap._difficulty],
            NoteJumpSpeed = beatmap._noteJumpMovementSpeed,
            SpawnOffset = beatmap._noteJumpStartBeatOffset
        };

        Debug.Log($"Loading json from {beatmap._beatmapFilename}");

        string location = Path.Combine(directory, beatmap._beatmapFilename);
        string json = await ReadFileAsync(location);

        Debug.Log($"Parsing {beatmap._beatmapFilename}");
        output.beatmapDifficulty = ParseBeatmapFromJson(json);

        return output;
    }


    public static BeatmapDifficulty ParseBeatmapFromJson(string json)
    {
        BeatmapDifficulty difficulty = new BeatmapDifficulty();
        BeatmapVersion beatmapVersion = new BeatmapVersion
        {
            version = "",
            _version = ""
        };

        try
        {
            beatmapVersion = JsonUtility.FromJson<BeatmapVersion>(json);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unable to parse difficulty version with error: {err.Message}, {err.StackTrace}.");
            return difficulty;
        }

        try
        {
            if(beatmapVersion.version == "3.0.0" || beatmapVersion.version == "3.1.0")
            {
                //Parse the difficulty file
                Debug.Log("Parsing map in V3 format.");
                difficulty = JsonUtility.FromJson<BeatmapDifficulty>(json);
            }
            else if(beatmapVersion._version == "2.9.0" || beatmapVersion._version == "2.6.0" || beatmapVersion._version == "2.5.0" || beatmapVersion._version == "2.2.0" || beatmapVersion._version == "2.0.0")
            {
                Debug.Log("Parsing map in V2 format.");

                BeatmapDifficultyV2 v2Diff = JsonUtility.FromJson<BeatmapDifficultyV2>(json);
                difficulty = BeatmapUtility.AddNullsDifficultyV2(v2Diff).ConvertToV3();
            }
            else
            {
                Debug.LogWarning("Unable to match map version. The map is either broken or in an unsupported version.");

                Debug.Log("Trying to fallback load map in V3 format.");
                BeatmapDifficulty v3Diff = JsonUtility.FromJson<BeatmapDifficulty>(json);

                if(v3Diff.colorNotes != null)
                {
                    Debug.Log("Fallback succeeded in V3.");
                    difficulty = v3Diff;
                }
                else
                {
                    Debug.Log("Fallback failed in V3, trying V2.");
                    BeatmapDifficultyV2 v2Diff = JsonUtility.FromJson<BeatmapDifficultyV2>(json);

                    if(v2Diff._notes != null)
                    {
                        Debug.Log("Fallback succeeded in V2.");
                        difficulty = BeatmapUtility.AddNullsDifficultyV2(v2Diff).ConvertToV3();
                    }
                    else
                    {
                        Debug.LogWarning("Difficulty failed to load due to unsupported or missing version.");
                        difficulty = new BeatmapDifficulty();
                    }
                }
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unable to parse difficulty file with error: {err.Message}, {err.StackTrace}.");
            difficulty = new BeatmapDifficulty();
        }

        difficulty = BeatmapUtility.AddNullsDifficulty(difficulty);
        Debug.Log($"Parsed difficulty with {difficulty.colorNotes.Length} notes, {difficulty.bombNotes.Length} bombs, and {difficulty.obstacles.Length} walls.");
        return difficulty;
    }


    public static Task<string> ReadFileAsync(string location)
    {
        string text = "";

        if(!File.Exists(location))
        {
            Debug.LogWarning("Trying to read a file that doesn't exist.");
            return Task.FromResult(text);
        }

        try
        {
            text = File.ReadAllText(location);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unable to read the text from file with error: {err.Message}, {err.StackTrace}.");
            text = "";
        }

        return Task.FromResult(text);
    }
}