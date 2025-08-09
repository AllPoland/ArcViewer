using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

public static class LightshowLoader
{
    public static async Task<Dictionary<string, BeatmapLightshowV4>> GetLightshowsAsync(BeatmapInfo info, string directory = null, ZipArchive archive = null)
    {
        if(ReplayManager.IsReplayMode)
        {
            return await LoadLightshowReplay(info, directory, archive);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        return await LoadLightshowsAsync(info, directory, archive);
#else
        if(SettingsManager.GetBool("concurrentloading", false))
        {
            return await LoadLightshowsConcurrent(info, directory, archive);
        }
        else return await LoadLightshowsAsync(info, directory, archive);
#endif
    }


    private static async Task<Dictionary<string, BeatmapLightshowV4>> LoadLightshowReplay(BeatmapInfo info, string directory = null, ZipArchive archive = null)
    {
        Debug.Log("Loading single lightshow for replay.");

        DifficultyCharacteristic replayCharacteristic = BeatmapInfo.CharacteristicFromString(ReplayManager.CurrentReplay.info.mode);
        DifficultyRank replayDiffRank = BeatmapInfo.DifficultyRankFromString(ReplayManager.CurrentReplay.info.difficulty);

        foreach(DifficultyBeatmap beatmap in info.difficultyBeatmaps)
        {
            string characteristicName = beatmap.characteristic;
            DifficultyCharacteristic characteristic = BeatmapInfo.CharacteristicFromString(characteristicName);

            if(characteristic != replayCharacteristic)
            {
                //This characteristic doesn't match the replay, so we don't need it
                continue;
            }

            DifficultyRank beatmapRank = BeatmapInfo.DifficultyRankFromString(beatmap.difficulty);
            if(beatmapRank != replayDiffRank)
            {
                //This diff doesn't match the replay, so we don't need it
                continue;
            }

            BeatmapLightshowV4 lightshow = await LoadLightshow(beatmap, directory, archive);

            //Return just this singular lightshow
            return new Dictionary<string, BeatmapLightshowV4> {{beatmap.lightshowDataFilename, lightshow}};
        }

        //If we exited the loop without loading a difficulty, it means we didn't find a match
        Debug.LogWarning($"No matching difficulty found for {replayCharacteristic}, {replayDiffRank}!");
        ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Found no matching difficulty for the replay!");

        return null;
    }


    private static async Task<Dictionary<string, BeatmapLightshowV4>> LoadLightshowsAsync(BeatmapInfo info, string directory = null, ZipArchive archive = null)
    {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        Debug.Log("Loading lightshows asynchronously.");
        Dictionary<string, BeatmapLightshowV4> lightshows = null;

        foreach(DifficultyBeatmap beatmap in info.difficultyBeatmaps)
        {
            if(lightshows?.ContainsKey(beatmap.lightshowDataFilename) ?? false)
            {
                //Don't load duplicate lightshows
                continue;
            }

            BeatmapLightshowV4 lightshow = await LoadLightshow(beatmap, directory, archive);
            if(lightshow != null)
            {
                lightshows ??= new Dictionary<string, BeatmapLightshowV4>();
                lightshows[beatmap.lightshowDataFilename] = lightshow;
            }
        }

        stopwatch.Stop();
        Debug.Log($"Lightshow loading took {stopwatch.ElapsedMilliseconds}ms.");

        return lightshows;
    }


//Suppress warnings about a lack of await when building for WebGL
#pragma warning disable 1998
    private static async Task<Dictionary<string, BeatmapLightshowV4>> LoadLightshowsConcurrent(BeatmapInfo info, string directory = null, ZipArchive archive = null)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new System.InvalidOperationException("Concurrent lightshow loading doesn't work in WebGL!");
#else
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        Debug.Log("Loading lightshows concurrently.");
        MapLoader.LoadingMessage = "Loading lightshows.";

        List<Task<BeatmapLightshowV4>> lightshowTasks = new List<Task<BeatmapLightshowV4>>();
        List<string> lightshowFilenames = new List<string>();
        foreach(DifficultyBeatmap beatmap in info.difficultyBeatmaps)
        {
            string lightshowFilename = beatmap.lightshowDataFilename;

            if(string.IsNullOrEmpty(lightshowFilename))
            {
                Debug.Log($"{beatmap.characteristic}, {beatmap.difficulty} has no lightshow attached.");
                continue;
            }

            if(lightshowFilenames.Contains(lightshowFilename))
            {
                //Don't load duplicate lightshows
                continue;
            }

            //Add each lightshow to a task list to run at once
            Debug.Log($"Adding {lightshowFilename} to task list.");

            if(!string.IsNullOrEmpty(directory))
            {
                //Loading from directory
                Task<BeatmapLightshowV4> newLightshowTask = Task.Run(() => GetLightshow(beatmap, directory));
                lightshowTasks.Add(newLightshowTask);
                lightshowFilenames.Add(lightshowFilename);
                continue;
            }

            //Loading from zip file
            using Stream lightshowStream = archive?.GetEntryCaseInsensitive(lightshowFilename)?.Open();
            if(lightshowStream != null)
            {
                //Read the byte array now because reading from the same ziparchive on multiple threads breaks shit
                byte[] lightshowData = FileUtil.StreamToBytes(lightshowStream);
                Task<BeatmapLightshowV4> newLightshowTask = Task.Run(() => GetLightshow(beatmap, null, lightshowData));
                lightshowTasks.Add(newLightshowTask);
                lightshowFilenames.Add(lightshowFilename);
            }
            else
            {
                Debug.LogWarning($"Unable to get lightshow data from {lightshowFilename}!");
            }
        }

        if(lightshowTasks.Count == 0)
        {
            //No lightshows were found
            return null;
        }

        //Wait until all the lightshows are finished
        await Task.WhenAll(lightshowTasks.ToArray());

        Dictionary<string, BeatmapLightshowV4> lightshows = new Dictionary<string, BeatmapLightshowV4>();
        for(int i = 0; i < lightshowTasks.Count; i++)
        {
            Task<BeatmapLightshowV4> task = lightshowTasks[i];
            if(task.Result == null)
            {
                Debug.LogWarning($"Failed to load lightshow from {lightshowFilenames[i]}!");
                ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Failed to load {lightshowFilenames[i]}!");
            }
            else
            {
                //Add the new lightshow
                lightshows[lightshowFilenames[i]] = task.Result;
            }

            task.Dispose();
        }

        stopwatch.Stop();
        Debug.Log($"Lightshow loading took {stopwatch.ElapsedMilliseconds}ms.");

        return lightshows;
#endif
    }
#pragma warning restore 1998


    private static async Task<BeatmapLightshowV4> LoadLightshow(DifficultyBeatmap beatmap, string directory = null, ZipArchive archive = null)
    {
        string lightshowFilename = beatmap.lightshowDataFilename;

        if(string.IsNullOrEmpty(lightshowFilename))
        {
            Debug.Log($"{beatmap.characteristic}, {beatmap.difficulty} has no lightshow attached.");
            return null;
        }

        BeatmapLightshowV4 lightshow;
        if(archive != null)
        {
            using Stream lightshowStream = archive.GetEntryCaseInsensitive(lightshowFilename)?.Open();
            byte[] lightshowData = FileUtil.StreamToBytes(lightshowStream);
            lightshow = await GetLightshow(beatmap, null, lightshowData);
        }
        else lightshow = await GetLightshow(beatmap, directory);

        if(lightshow == null)
        {
            Debug.LogWarning($"Failed to load lightshow from {lightshowFilename}!");
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Failed to load {lightshowFilename}!");
        }
        return lightshow;
    }


    private static async Task<BeatmapLightshowV4> GetLightshow(DifficultyBeatmap beatmap, string directory = null, byte[] lightshowData = null)
    {
        string lightshowFilename = beatmap.lightshowDataFilename;

        if(string.IsNullOrEmpty(lightshowFilename))
        {
            Debug.Log($"{beatmap.characteristic}, {beatmap.difficulty} has no lightshow attached.");
            return null;
        }

        Debug.Log($"Loading lightshow {lightshowFilename}");
        MapLoader.LoadingMessage = $"Loading {lightshowFilename}";

        if(!string.IsNullOrEmpty(directory))
        {
            return await JsonReader.LoadLightshowAsync(directory, lightshowFilename);
        }
        else if(lightshowData != null)
        {
            return ZipReader.GetLightshow(lightshowData);
        }
        else
        {
            Debug.LogWarning($"Unable to load {lightshowFilename}!");
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Unable to load {lightshowFilename}!");
            return null;
        }
    }
}
