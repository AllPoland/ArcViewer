using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

public class DifficultyLoader
{
    public static async Task<List<Difficulty>> GetDifficultiesAsync(BeatmapInfo info, string directory)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return await LoadDifficultiesAsync(info, directory);
#else
        if(SettingsManager.GetBool("concurrentloading"))
        {
            return await LoadDifficultiesConcurrent(info, directory);
        }
        else return await LoadDifficultiesAsync(info, directory);
#endif
    }


    public static async Task<List<Difficulty>> GetDifficultiesAsync(BeatmapInfo info, ZipArchive archive)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        return await LoadDifficultiesAsync(info, null, archive);
#else
        if(SettingsManager.GetBool("concurrentloading"))
        {
            return await LoadDifficultiesConcurrent(info, null, archive);
        }
        else return await LoadDifficultiesAsync(info, null, archive);
#endif
    }


    private static async Task<List<Difficulty>> LoadDifficultiesAsync(BeatmapInfo info, string directory = null, ZipArchive archive = null)
    {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        Debug.Log("Loading difficulties asynchronously.");
        List<Difficulty> difficulties = new List<Difficulty>();
        foreach(DifficultyBeatmapSet set in info._difficultyBeatmapSets)
        {
            string characteristicName = set._beatmapCharacteristicName;
            if(set._difficultyBeatmaps.Length == 0)
            {
                Debug.LogWarning($"{characteristicName} lists no difficulties!");
                continue;
            }

            DifficultyCharacteristic setCharacteristic = BeatmapInfo.CharacteristicFromString(characteristicName);

            int diffCount = 0;
            foreach(DifficultyBeatmap beatmap in set._difficultyBeatmaps)
            {
                MapLoader.LoadingMessage = $"Loading {beatmap._beatmapFilename}";
                Debug.Log($"Loading {beatmap._beatmapFilename}");

                //Yielding is a dumb and inconsistent way of allowing the loading text to update
                await Task.Yield();
                Difficulty newDifficulty = await LoadDifficultyAsync(beatmap, setCharacteristic, directory, archive);
                if(newDifficulty == null) continue;

                difficulties.Add(newDifficulty);
                diffCount++;
            }
            Debug.Log($"Finished loading {diffCount} difficulties in characteristic {characteristicName}.");
        }

        stopwatch.Stop();
        Debug.Log($"Difficulty loading took {stopwatch.ElapsedMilliseconds}ms.");
        return difficulties;
    }


//Suppress warnings about a lack of await when building for WebGL
#pragma warning disable 1998
    private static async Task<List<Difficulty>> LoadDifficultiesConcurrent(BeatmapInfo info, string directory = null, ZipArchive archive = null)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new System.InvalidOperationException("Concurrent difficulty loading doesn't work in WebGL!");
#else
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        Debug.Log("Loading difficulties concurrently.");
        MapLoader.LoadingMessage = "Loading difficulties";

        List<Task<Difficulty>> difficultyTasks = new List<Task<Difficulty>>();
        List<string> difficultyFilenames = new List<string>();
        foreach(DifficultyBeatmapSet set in info._difficultyBeatmapSets)
        {
            string characteristicName = set._beatmapCharacteristicName;
            if(set._difficultyBeatmaps.Length == 0)
            {
                Debug.LogWarning($"{characteristicName} lists no difficulties!");
                continue;
            }

            DifficultyCharacteristic setCharacteristic = BeatmapInfo.CharacteristicFromString(characteristicName);
            foreach(DifficultyBeatmap beatmap in set._difficultyBeatmaps)
            {
                string filename = beatmap._beatmapFilename;
                Debug.Log($"Adding {filename} to task list.");

                if(!string.IsNullOrEmpty(directory))
                {
                    //Loading map from directory
                    Task<Difficulty> newDiffTask = Task.Run(() => LoadDifficultyConcurrent(beatmap, setCharacteristic, directory));
                    difficultyTasks.Add(newDiffTask);
                    difficultyFilenames.Add(filename);
                    continue;
                }

                //Loading map from zip file
                using Stream diffStream = archive?.GetEntryCaseInsensitive(filename)?.Open();
                if(diffStream != null)
                {
                    //Read the byte array now because reading from the same ziparchive on multiple threads breaks shit
                    byte[] diffData = FileUtil.StreamToBytes(diffStream);
                    Task<Difficulty> newDiffTask = Task.Run(() => LoadDifficultyConcurrent(beatmap, setCharacteristic, null, diffData));
                    difficultyTasks.Add(newDiffTask);
                    difficultyFilenames.Add(filename);
                }
                else
                {
                    Debug.LogWarning($"Unable to get difficulty data from {filename}!");
                }
            }
            Debug.Log($"Added {set._difficultyBeatmaps.Length} difficulties in characteristic {characteristicName}.");
        }

        if(difficultyTasks.Count != difficultyFilenames.Count)
        {
            Debug.LogWarning($"Different amount of tasks and filenames listed! This shouldn't happen.");

            //Ensure there are at least as many filenames listed as tasks to avoid fatal errors
            while(difficultyFilenames.Count < difficultyTasks.Count)
            {
                difficultyFilenames.Add("{UnknownFile}");
            }
        }

        await Task.WhenAll(difficultyTasks.ToArray());

        List<Difficulty> difficulties = new List<Difficulty>();
        for(int i = 0; i < difficultyTasks.Count; i++)
        {
            Task<Difficulty> task = difficultyTasks[i];
            if(task.Result == null)
            {
                Debug.LogWarning($"Failed to load difficulty from {difficultyFilenames[i]}!");
            }
            else
            {
                difficulties.Add(task.Result);
            }
            task.Dispose();
        }

        stopwatch.Stop();
        Debug.Log($"Difficulty loading took {stopwatch.ElapsedMilliseconds}ms.");
        return difficulties;
#endif
    }
#pragma warning restore 1998


    private static async Task<Difficulty> LoadDifficultyAsync(DifficultyBeatmap beatmap, DifficultyCharacteristic characteristic, string directory = null, ZipArchive archive = null)
    {
        Difficulty difficulty;
        if(!string.IsNullOrEmpty(directory))
        {
            difficulty = await JsonReader.LoadDifficultyAsync(directory, beatmap);
        }
        else if(archive != null)
        {
            difficulty = ZipReader.GetDifficulty(archive, beatmap);
        }
        else difficulty = null;

        if(difficulty == null)
        {
            Debug.LogWarning($"Unable to load {beatmap._beatmapFilename}!");
            return null;
        }

        difficulty.characteristic = characteristic;
        FillCustomDifficultyData(ref difficulty, beatmap);
        return difficulty;
    }


    private static async Task<Difficulty> LoadDifficultyConcurrent(DifficultyBeatmap beatmap, DifficultyCharacteristic characteristic, string directory = null, byte[] diffData = null)
    {
        Difficulty difficulty;
        if(!string.IsNullOrEmpty(directory))
        {
            difficulty = await JsonReader.LoadDifficultyAsync(directory, beatmap);
        }
        else if(diffData != null && diffData.Length > 0)
        {
            difficulty = ZipReader.GetDifficulty(diffData, beatmap);
        }
        else difficulty = null;

        if(difficulty == null)
        {
            Debug.LogWarning($"Unable to load {beatmap._beatmapFilename}!");
            return null;
        }

        difficulty.characteristic = characteristic;
        FillCustomDifficultyData(ref difficulty, beatmap);
        return difficulty;
    }


    public static void FillCustomDifficultyData(ref Difficulty difficulty, DifficultyBeatmap beatmap)
    {
        difficulty.requirements = beatmap._customData?._requirements ?? new string[0];
        difficulty.Label = beatmap._customData?._difficultyLabel ?? Difficulty.DiffLabelFromRank(difficulty.difficultyRank);
    }
}