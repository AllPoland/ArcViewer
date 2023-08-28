using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

public static class DifficultyLoader
{
    public static async Task<List<Difficulty>> GetDifficultiesAsync(BeatmapInfo info, string directory)
    {
        if(ReplayManager.IsReplayMode)
        {
            return await LoadDifficultyReplay(info, directory);
        }

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
        if(ReplayManager.IsReplayMode)
        {
            return await LoadDifficultyReplay(info, null, archive);
        }

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


    private static async Task<List<Difficulty>> LoadDifficultyReplay(BeatmapInfo info, string directory = null, ZipArchive archive = null)
    {
        Debug.Log("Loading single difficulty for replay.");

        DifficultyCharacteristic replayCharacteristic = BeatmapInfo.CharacteristicFromString(ReplayManager.CurrentReplay.info.mode);
        DifficultyRank replayDiffRank = BeatmapInfo.DifficultyRankFromString(ReplayManager.CurrentReplay.info.difficulty);

        foreach(DifficultyBeatmapSet set in info._difficultyBeatmapSets)
        {
            string characteristicName = set._beatmapCharacteristicName;
            DifficultyCharacteristic setCharacteristic = BeatmapInfo.CharacteristicFromString(characteristicName);

            if(setCharacteristic != replayCharacteristic)
            {
                //This characteristic doesn't match the replay, so we don't need it
                continue;
            }

            if(set._difficultyBeatmaps.Length == 0)
            {
                Debug.LogWarning($"{characteristicName} lists no difficulties!");
                break;
            }

            foreach(DifficultyBeatmap beatmap in set._difficultyBeatmaps)
            {
                DifficultyRank beatmapRank = BeatmapInfo.DifficultyRankFromString(beatmap._difficulty);
                if(beatmapRank != replayDiffRank)
                {
                    //This diff doesn't match the replay, so we don't need it
                    continue;
                }

                MapLoader.LoadingMessage = $"Loading {beatmap._beatmapFilename}";
                Debug.Log($"Loading {beatmap._beatmapFilename}");

                await Task.Yield();
                Difficulty newDifficulty = await LoadDifficultyFile(info, beatmap, setCharacteristic, directory, archive);
                if(newDifficulty == null) break;

                //Return just this singular difficulty
                return new List<Difficulty> { newDifficulty };
            }
        }

        //If we exited the loop without loading a difficulty, it means we didn't find a match
        Debug.LogWarning($"No matching difficulty found for {replayCharacteristic}, {replayDiffRank}!");
        ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Found no matching difficulty for the replay!");

        return null;
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
                //Task.Delay() doesn't work on WebGL for reasons
                await Task.Yield();
                Difficulty newDifficulty = await LoadDifficultyFile(info, beatmap, setCharacteristic, directory, archive);
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
                //Add each difficulty to a task list to run at once
                string filename = beatmap._beatmapFilename;
                Debug.Log($"Adding {filename} to task list.");

                if(!string.IsNullOrEmpty(directory))
                {
                    //Loading map from directory
                    Task<Difficulty> newDiffTask = Task.Run(() => LoadDifficulty(info, beatmap, setCharacteristic, directory));
                    difficultyTasks.Add(newDiffTask);
                    continue;
                }

                //Loading map from zip file
                using Stream diffStream = archive?.GetEntryCaseInsensitive(filename)?.Open();
                if(diffStream != null)
                {
                    //Read the byte array now because reading from the same ziparchive on multiple threads breaks shit
                    byte[] diffData = FileUtil.StreamToBytes(diffStream);
                    Task<Difficulty> newDiffTask = Task.Run(() => LoadDifficulty(info, beatmap, setCharacteristic, null, diffData));
                    difficultyTasks.Add(newDiffTask);
                }
                else
                {
                    Debug.LogWarning($"Unable to get difficulty data from {filename}!");
                }
            }
            Debug.Log($"Added {set._difficultyBeatmaps.Length} difficulties in characteristic {characteristicName}.");
        }

        //Run all loading tasks concurrently
        await Task.WhenAll(difficultyTasks.ToArray());

        List<Difficulty> difficulties = new List<Difficulty>();
        for(int i = 0; i < difficultyTasks.Count; i++)
        {
            Task<Difficulty> task = difficultyTasks[i];
            if(task.Result == null)
            {
                Debug.LogWarning($"Failed to load difficulty from task {i}!");
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


    private static async Task<Difficulty> LoadDifficultyFile(BeatmapInfo info, DifficultyBeatmap beatmap, DifficultyCharacteristic characteristic, string directory = null, ZipArchive archive = null)
    {
        if(!string.IsNullOrEmpty(directory))
        {
            return await LoadDifficulty(info, beatmap, characteristic, directory);
        }
        else if(archive != null)
        {
            using Stream diffStream = archive.GetEntryCaseInsensitive(beatmap._beatmapFilename).Open();
            byte[] diffData = FileUtil.StreamToBytes(diffStream);
            return await LoadDifficulty(info, beatmap, characteristic, null, diffData);
        }
        else
        {
            Debug.LogWarning($"Unable to load {beatmap._beatmapFilename}!");
            return null;
        }
    }


    private static async Task<Difficulty> LoadDifficulty(BeatmapInfo info, DifficultyBeatmap beatmap, DifficultyCharacteristic characteristic, string directory = null, byte[] diffData = null)
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
        difficulty.environmentName = GetDifficultyEnvironmentName(info, beatmap);
        difficulty.colorScheme = GetDifficultyColorScheme(info, beatmap);

        FillCustomDifficultyData(ref difficulty, beatmap);
        return difficulty;
    }


    private static NullableColorPalette GetDifficultyColorScheme(BeatmapInfo info, DifficultyBeatmap beatmap)
    {
        ColorSchemeContainer[] colorSchemes = info._colorSchemes;
        if(colorSchemes == null)
        {
            //No color schemes are present
            return null;
        }

        int colorSchemeIndex = beatmap._beatmapColorSchemeIdx;
        if(colorSchemeIndex < 0 || colorSchemeIndex >= colorSchemes.Length)
        {
            //No color scheme of this index
            return null;
        }

        ColorSchemeContainer colorSchemeContainer = colorSchemes[colorSchemeIndex];
        if(!colorSchemeContainer.useOverride)
        {
            //This color scheme shouldn't override default colors
            return null;
        }

        return colorSchemeContainer.colorScheme.GetPalette();
    }


    private static string GetDifficultyEnvironmentName(BeatmapInfo info, DifficultyBeatmap beatmap)
    {
        string[] environmentNames = info._environmentNames;
        if(environmentNames == null)
        {
            //No environment list means we use the default listed in the info base object
            return info._environmentName;
        }

        int environmentIndex = beatmap._beatmapColorSchemeIdx;
        if(environmentIndex < 0 || environmentIndex >= environmentNames.Length)
        {
            //The environment list doesn't contain this index, so use default
            return info._environmentName;
        }

        return environmentNames[environmentIndex];
    }


    private static void FillCustomDifficultyData(ref Difficulty difficulty, DifficultyBeatmap beatmap)
    {
        difficulty.requirements = beatmap._customData?._requirements ?? new string[0];
        difficulty.label = beatmap._customData?._difficultyLabel ?? Difficulty.DiffLabelFromRank(difficulty.difficultyRank);
        difficulty.songCoreColors = ColorPaletteFromCustomData(beatmap._customData);
    }


    private static NullableColorPalette ColorPaletteFromCustomData(CustomDifficultyData customData)
    {
        if(customData == null)
        {
            return null;
        }

        return new NullableColorPalette
        {
            LeftNoteColor = customData._colorLeft?.GetColor(),
            RightNoteColor = customData._colorRight?.GetColor(),
            LightColor1 = customData._envColorLeft?.GetColor(),
            LightColor2 = customData._envColorRight?.GetColor(),
            WhiteLightColor = customData._envColorWhite?.GetColor(),
            BoostLightColor1 = customData._envColorLeftBoost?.GetColor(),
            BoostLightColor2 = customData._envColorRightBoost?.GetColor(),
            BoostWhiteLightColor = customData._envColorWhiteBoost?.GetColor(),
            WallColor = customData._obstacleColor?.GetColor()
        };
    }
}