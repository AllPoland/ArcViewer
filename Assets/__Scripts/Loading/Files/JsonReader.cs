using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class JsonReader
{
    public static async Task<BeatmapInfo> LoadInfoAsync(string location)
    {
        Debug.Log("Loading text from Info.dat");
        string json = await ReadFileAsync(location);

        if(json == "")
        {
            return null;
        }

        Debug.Log("Parsing Info.dat.");
        BeatmapInfo info = ParseInfoFromJson(json);

        return info;
    }


    public static BeatmapInfo ParseInfoFromJson(string json)
    {
        BeatmapInfo info;

        try
        {
            info = JsonUtility.FromJson<BeatmapInfo>(json);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unable to parse info from json with error: {err.Message}, {err.StackTrace}.");
            info = null;
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
            difficultyRank = MapLoader.DiffValueFromString[beatmap._difficulty],
            NoteJumpSpeed = beatmap._noteJumpMovementSpeed,
            SpawnOffset = beatmap._noteJumpStartBeatOffset
        };
        output.Label = beatmap._customData?._difficultyLabel ?? Difficulty.DiffLabelFromRank(output.difficultyRank);

        string filename = beatmap._beatmapFilename;
        Debug.Log($"Loading json from {filename}");

        string location = Path.Combine(directory, filename);
        string json = await ReadFileAsync(location);

        if(json == "")
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Unable to load {filename}!");
            return null;
        }

        Debug.Log($"Parsing {filename}");
        output.beatmapDifficulty = ParseBeatmapFromJson(json);

        return output;
    }


    public static BeatmapDifficulty ParseBeatmapFromJson(string json)
    {
        BeatmapDifficulty difficulty = new BeatmapDifficulty();
        BeatmapVersion beatmapVersion;

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
            string[] v3Versions = {"3.0.0", "3.1.0", "3.2.0"};
            string[] v2Versions = {"2.0.0", "2.2.0", "2.5.0", "2.6.0"};

            if(Array.IndexOf(v3Versions, beatmapVersion.version) > -1)
            {
                //Parse the difficulty file
                Debug.Log("Parsing map in V3 format.");
                difficulty = JsonUtility.FromJson<BeatmapDifficulty>(json);
            }
            else if(Array.IndexOf(v2Versions, beatmapVersion._version) > -1)
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
                        ErrorHandler.Instance.QueuePopup(ErrorType.Warning, "Unable to find difficulty version!");
                        Debug.LogWarning("Difficulty failed to load due to unsupported or missing version!");
                        return new BeatmapDifficulty();
                    }
                }
            }
        }
        catch(Exception err)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, "Unable to parse difficulty!");
            Debug.LogWarning($"Unable to parse difficulty file with error: {err.Message}, {err.StackTrace}.");
            return new BeatmapDifficulty();
        }

        difficulty = BeatmapUtility.AddNullsDifficulty(difficulty);
        Debug.Log($"Parsed difficulty with {difficulty.colorNotes.Length} notes, {difficulty.bombNotes.Length} bombs, and {difficulty.obstacles.Length} walls.");
        return difficulty;
    }


    public static async Task<string> ReadFileAsync(string location)
    {
        string text = "";

        if(!File.Exists(location))
        {
            Debug.LogWarning("Trying to read a file that doesn't exist!");
            return text;
        }

        try
        {
            text = await File.ReadAllTextAsync(location);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unable to read the text from file with error: {err.Message}, {err.StackTrace}.");
            text = "";
        }

        return text;
    }
}