using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public static class JsonReader
{
    public static Regex VersionRx = new Regex(@"version""\s*:\s*""(\d\.?)*", RegexOptions.Compiled);


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
            info = DeserializeObject<BeatmapInfo>(json);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unable to parse info from json with error: {err.Message}, {err.StackTrace}.");
            return null;
        }

        info?.AddNulls();
        return info;
    }


    public static async Task<Difficulty> LoadDifficultyAsync(string directory, DifficultyBeatmap beatmap)
    {
        Difficulty output = new Difficulty
        {
            difficultyRank = MapLoader.DiffValueFromString[beatmap._difficulty],
            noteJumpSpeed = beatmap._noteJumpMovementSpeed,
            spawnOffset = beatmap._noteJumpStartBeatOffset
        };

        string filename = beatmap._beatmapFilename;
        Debug.Log($"Loading json from {filename}");

        string location = Path.Combine(directory, filename);
        string json = await ReadFileAsync(location);

        if(string.IsNullOrEmpty(json))
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Unable to load {filename}!");
            return null;
        }

        Debug.Log($"Parsing {filename}");
        output.beatmapDifficulty = ParseBeatmapFromJson(json, filename);

        return output;
    }


    public static BeatmapDifficulty ParseBeatmapFromJson(string json, string filename = "{UnknownDifficulty}")
    {
        BeatmapDifficulty difficulty = new BeatmapDifficulty();

        try
        {
            string[] v3Versions = {"3.0.0", "3.1.0", "3.2.0"};
            string[] v2Versions = {"2.0.0", "2.2.0", "2.5.0", "2.6.0"};

            Match match = VersionRx.Match(json);

            //Get only the version number
            string versionNumber = match.Value.Split('"').Last();
            Debug.Log($"{filename} is version: {versionNumber}");

            //Search for a matching version and parse the correct map format
            if(v3Versions.Contains(versionNumber))
            {
                Debug.Log($"Parsing {filename} in V3 format.");
                difficulty = DeserializeObject<BeatmapDifficulty>(json);
            }
            else if(v2Versions.Contains(versionNumber))
            {
                Debug.Log($"Parsing {filename} in V2 format.");

                BeatmapDifficultyV2 v2Diff = DeserializeObject<BeatmapDifficultyV2>(json);
                difficulty = v2Diff.ConvertToV3();
            }
            else
            {
                Debug.LogWarning($"Unable to match map version for {filename}. The map has either a missing or unsupported version.");

                Debug.Log($"Trying to fallback load {filename} in V3 format.");
                BeatmapDifficulty v3Diff = DeserializeObject<BeatmapDifficulty>(json);

                if(v3Diff?.HasObjects ?? false)
                {
                    Debug.Log($"Fallback for {filename} succeeded in V3.");
                    difficulty = v3Diff;
                }
                else
                {
                    Debug.Log($"Fallback for {filename} failed in V3, trying V2.");
                    BeatmapDifficultyV2 v2Diff = DeserializeObject<BeatmapDifficultyV2>(json);

                    if(v2Diff?.HasObjects ?? false)
                    {
                        Debug.Log($"Fallback for {filename} succeeded in V2.");
                        difficulty = v2Diff.ConvertToV3();
                    }
                    else
                    {
                        ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Unable to find difficulty version from {filename}!");
                        Debug.LogWarning($"{filename} is in an unsupported or missing version!");
                        return new BeatmapDifficulty();
                    }
                }
            }
        }
        catch(Exception err)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Unable to parse {filename}!");
            Debug.LogWarning($"Unable to parse {filename} file with error: {err.Message}, {err.StackTrace}.");
            return new BeatmapDifficulty();
        }

        difficulty.AddNulls();
        Debug.Log($"Parsed {filename} with {difficulty.colorNotes.Length} notes, {difficulty.bombNotes.Length} bombs, {difficulty.obstacles.Length} walls, {difficulty.sliders.Length} arcs, and {difficulty.burstSliders.Length} chains.");
        return difficulty;
    }


    public static async Task<string> ReadFileAsync(string location)
    {
        if(!File.Exists(location))
        {
            Debug.LogWarning("Trying to read a file that doesn't exist!");
            return "";
        }

        try
        {
            string text = await File.ReadAllTextAsync(location);
            return text;
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unable to read the text from file with error: {err.Message}, {err.StackTrace}.");
            return "";
        }
    }


    public static T DeserializeObject<T>(string json) => JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
    {
        Error = HandleDeserializationError
    });


    public static void HandleDeserializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
    {
        Debug.LogWarning($"Error parsing json: {args.ErrorContext.Error.Message}");
        args.ErrorContext.Handled = true;
    }
}