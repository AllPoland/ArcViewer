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


    public static async Task<BeatmapInfo> LoadInfoAsync(string directory)
    {
        Debug.Log("Loading text from Info.dat");

        string infoPath = Path.Combine(directory, "Info.dat");
        string json = await ReadFileAsync(infoPath);

        if(json == "")
        {
            Debug.LogWarning("Empty data in Info.dat!");
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
            Match match = VersionRx.Match(json);

            //Get only the version number
            string versionNumber = match.Value.Split('"').Last();
            Debug.Log($"Info.dat is version: {versionNumber}");

            string[] v4Versions = {"4.0.0", "4.0.1"};
            string[] v2Versions = {"2.0.0", "2.1.0"};
            
            if(v4Versions.Contains(versionNumber))
            {
                Debug.Log("Parsing Info.dat in V4 format.");
                info = DeserializeObject<BeatmapInfo>(json);
            }
            else if(v2Versions.Contains(versionNumber))
            {
                Debug.Log("Parsing Info.dat in V2 format.");
                BeatmapInfoV2 v2Info = DeserializeObject<BeatmapInfoV2>(json);
                info = v2Info.ConvertToV4();
            }
            else
            {
                Debug.LogWarning("Info.dat has missing or unsupported version.");
                info = ParseInfoFallback(json);
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unable to parse info from json with error: {err.Message}, {err.StackTrace}.");
            return null;
        }

        return info;
    }


    private static BeatmapInfo ParseInfoFallback(string json)
    {
        Debug.Log("Trying to fallback load Info.dat in V4 format.");
        BeatmapInfo infoV4 = DeserializeObject<BeatmapInfo>(json);

        if(infoV4?.HasFields ?? false)
        {
            Debug.Log("Fallback for Info.dat succeeded in V4.");
            return infoV4;
        }

        Debug.Log("Fallback for Info.dat failed in V4, trying V2.");
        BeatmapInfoV2 infoV2 = DeserializeObject<BeatmapInfoV2>(json);

        if(infoV2?.HasFields ?? false)
        {
            Debug.Log("Fallback for Info.dat succeeded in V2.");
            return infoV2.ConvertToV4();
        }

        Debug.LogWarning("Info.dat is in an unsupported or missing version!");
        return null;
    }


    public static async Task<BeatmapBpmEvent[]> GetBpmEventsAsync(BeatmapInfo info, string directory)
    {
        if(string.IsNullOrEmpty(info?.audio?.audioDataFilename))
        {
            //No audio data is listed by the info
            return null;
        }

        string audioDataFilename = info.audio.audioDataFilename;
        try
        {
            Debug.Log($"Loading {audioDataFilename}");

            string audioPath = Path.Combine(directory, audioDataFilename);
            string audioJson = await ReadFileAsync(audioPath);

            return ParseBpmEventsFromAudioJson(audioJson);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Audio data loading failed with error: {err.Message}, {err.StackTrace}");
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, "Failed to load audio metadata! BPM might not line up.");
            return null;
        }
    }


    public static BeatmapBpmEvent[] ParseBpmEventsFromAudioJson(string json)
    {
        AudioDataV4 audioData = DeserializeObject<AudioDataV4>(json);

        if(audioData?.bpmData == null)
        {
            Debug.LogWarning("Empty bpm data in audio file!");
            return null;
        }

        return audioData.GetBpmChanges();
    }


    public static async Task<BeatmapLightshowV4> LoadLightshowAsync(string directory, string lightshowFilename)
    {
        try
        {
            string lightshowPath = Path.Combine(directory, lightshowFilename);
            string json = await ReadFileAsync(lightshowPath);

            if(json == null)
            {
                Debug.LogWarning($"Empty data in {lightshowFilename}!");
                return null;
            }

            return DeserializeObject<BeatmapLightshowV4>(json);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to load {lightshowFilename} with error: {err.Message}, {err.StackTrace}");
            return null;
        }
    }


    public static async Task<Difficulty> LoadDifficultyAsync(string directory, DifficultyBeatmap beatmap, LoadedMapData mapData)
    {
        Difficulty output = new Difficulty
        {
            difficultyRank = BeatmapInfo.DifficultyRankFromString(beatmap.difficulty),
            noteJumpSpeed = beatmap.noteJumpMovementSpeed,
            spawnOffset = beatmap.noteJumpStartBeatOffset
        };

        string filename = beatmap.beatmapDataFilename;
        Debug.Log($"Loading json from {filename}");

        string location = Path.Combine(directory, filename);
        string json = await ReadFileAsync(location);

        if(string.IsNullOrEmpty(json))
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Unable to load {filename}!");
            return null;
        }

        Debug.Log($"Parsing {filename}");
        output.beatmapDifficulty = GetBeatmapDifficulty(json, beatmap, mapData);

        return output;
    }


    public static BeatmapDifficulty GetBeatmapDifficulty(string json, DifficultyBeatmap beatmap, LoadedMapData mapData)
    {
        string filename = beatmap.beatmapDataFilename;

        BeatmapDifficulty difficulty;

        try
        {
            string[] v4Versions = {"4.0.0", "4.1.0"};
            string[] v3Versions = {"3.0.0", "3.1.0", "3.2.0", "3.3.0"};
            string[] v2Versions = {"2.0.0", "2.2.0", "2.5.0", "2.6.0"};

            Match match = VersionRx.Match(json);

            //Get only the version number
            string versionNumber = match.Value.Split('"').Last();
            Debug.Log($"{filename} is version: {versionNumber}");

            //Search for a matching version and parse the correct map format
            if(v4Versions.Contains(versionNumber))
            {
                Debug.Log($"Parsing {filename} in V4 format.");
                BeatmapDifficultyV4 beatmapData = DeserializeObject<BeatmapDifficultyV4>(json);
                difficulty = new BeatmapWrapperV4(beatmapData, mapData.GetLightshow(beatmap.lightshowDataFilename), mapData.BpmEvents);
            }
            else if(v3Versions.Contains(versionNumber))
            {
                Debug.Log($"Parsing {filename} in V3 format.");
                BeatmapDifficultyV3 beatmapData = DeserializeObject<BeatmapDifficultyV3>(json);
                difficulty = new BeatmapWrapperV3(beatmapData);
            }
            else if(v2Versions.Contains(versionNumber))
            {
                Debug.Log($"Parsing {filename} in V2 format.");
                BeatmapDifficultyV2 beatmapData = DeserializeObject<BeatmapDifficultyV2>(json);
                difficulty = new BeatmapWrapperV3(beatmapData.ConvertToV3());
            }
            else
            {
                Debug.LogWarning($"Unable to match map version for {filename}. The map has either a missing or unsupported version.");
                difficulty = ParseBeatmapFallback(json, beatmap, mapData);
            }
        }
        catch(Exception err)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Unable to parse {filename}!");
            Debug.LogWarning($"Unable to parse {filename} file with error: {err.Message}, {err.StackTrace}.");
            return BeatmapDifficulty.GetDefault();
        }

        Debug.Log($"Parsed {filename} with {difficulty.Notes.Length} notes, {difficulty.Bombs.Length} bombs, {difficulty.Walls.Length} walls, {difficulty.Arcs.Length} arcs, {difficulty.Chains.Length} chains.");
        return difficulty;
    }


    private static BeatmapDifficulty ParseBeatmapFallback(string json, DifficultyBeatmap beatmap, LoadedMapData mapData)
    {
        string filename = beatmap.beatmapDataFilename;

        Debug.Log($"Trying to fallback load {filename} in V4 format.");
        BeatmapDifficultyV4 v4Diff = DeserializeObject<BeatmapDifficultyV4>(json);

        if(v4Diff?.HasObjects ?? false)
        {
            Debug.Log($"Fallback for {filename} succeeded in V4.");
            return new BeatmapWrapperV4(v4Diff, mapData.GetLightshow(beatmap.lightshowDataFilename), mapData.BpmEvents);
        }

        Debug.Log($"Fallback for {filename} failed in V4, trying V3.");
        BeatmapDifficultyV3 v3Diff = DeserializeObject<BeatmapDifficultyV3>(json);

        if(v3Diff?.HasObjects ?? false)
        {
            Debug.Log($"Fallback for {filename} succeeded in V3.");
            return new BeatmapWrapperV3(v3Diff);
        }

        Debug.Log($"Fallback for {filename} failed in V3, trying V2.");
        BeatmapDifficultyV2 v2Diff = DeserializeObject<BeatmapDifficultyV2>(json);

        if(v2Diff?.HasObjects ?? false)
        {
            Debug.Log($"Fallback for {filename} succeeded in V2.");
            return new BeatmapWrapperV3(v2Diff.ConvertToV3());
        }

        ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Unable to find difficulty version for {filename}!");
        Debug.LogWarning($"{filename} is in an unsupported or missing version!");
        return BeatmapDifficulty.GetDefault();
    }


    public static async Task<string> ReadFileAsync(string location)
    {
        if(!File.Exists(location))
        {
            Debug.LogWarning($"Trying to read {Path.GetFileName(location)} which doesn't exist!");
            return "";
        }

        try
        {
            string text = await File.ReadAllTextAsync(location);
            return text;
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Unable to read the text from {Path.GetFileName(location)} with error: {err.Message}, {err.StackTrace}.");
            return "";
        }
    }


    public static T DeserializeObject<T>(string json) => JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings
    {
        Error = HandleDeserializationError
    });


    public static void HandleDeserializationError(object sender, Newtonsoft.Json.Serialization.ErrorEventArgs args)
    {
        Debug.LogWarning($"Error parsing json: {args.ErrorContext.Error.Message}, {args.ErrorContext.Error.StackTrace}");
        args.ErrorContext.Handled = true;
    }
}