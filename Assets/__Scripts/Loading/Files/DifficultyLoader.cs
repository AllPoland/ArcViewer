using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

public static class DifficultyLoader
{
    public static async Task<List<Difficulty>> GetDifficultiesAsync(LoadedMapData mapData, string directory = null, ZipArchive archive = null)
    {
        if(ReplayManager.IsReplayMode)
        {
            return await LoadDifficultyReplay(mapData, directory, archive);
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        return await LoadDifficultiesAsync(mapData, directory, archive);
#else
        if(SettingsManager.GetBool("concurrentloading", false))
        {
            return await LoadDifficultiesConcurrent(mapData, directory, archive);
        }
        else return await LoadDifficultiesAsync(mapData, directory, archive);
#endif
    }


    private static async Task<List<Difficulty>> LoadDifficultyReplay(LoadedMapData mapData, string directory = null, ZipArchive archive = null)
    {
        Debug.Log("Loading single difficulty for replay.");

        DifficultyCharacteristic replayCharacteristic = BeatmapInfo.CharacteristicFromString(ReplayManager.CurrentReplay.info.mode);
        DifficultyRank replayDiffRank = BeatmapInfo.DifficultyRankFromString(ReplayManager.CurrentReplay.info.difficulty);

        foreach(DifficultyBeatmap beatmap in mapData.Info.difficultyBeatmaps)
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

            MapLoader.LoadingMessage = $"Loading {beatmap.beatmapDataFilename}";

            Debug.Log($"Loading {beatmap.characteristic}, {beatmap.difficulty}");
            Difficulty newDifficulty = await LoadDifficultyFile(mapData, beatmap, characteristic, directory, archive);
            if(newDifficulty == null) break;

            //Return just this singular difficulty
            return new List<Difficulty> { newDifficulty };
        }

        //If we exited the loop without loading a difficulty, it means we didn't find a match
        Debug.LogWarning($"No matching difficulty found for {replayCharacteristic}, {replayDiffRank}!");
        ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Found no matching difficulty for the replay!");

        return null;
    }


    private static async Task<List<Difficulty>> LoadDifficultiesAsync(LoadedMapData mapData, string directory = null, ZipArchive archive = null)
    {
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        Debug.Log("Loading difficulties asynchronously.");
        List<Difficulty> difficulties = new List<Difficulty>();
        foreach(DifficultyBeatmap beatmap in mapData.Info.difficultyBeatmaps)
        {
            string characteristicName = beatmap.characteristic;

            DifficultyCharacteristic characteristic = BeatmapInfo.CharacteristicFromString(characteristicName);

            MapLoader.LoadingMessage = $"Loading {beatmap.beatmapDataFilename}";
            Debug.Log($"Loading {beatmap.characteristic}, {beatmap.difficulty}");

            //Yielding is a dumb and inconsistent way of allowing the loading text to update
            //Task.Delay() doesn't work on WebGL for reasons
            await Task.Yield();
            Difficulty newDifficulty = await LoadDifficultyFile(mapData, beatmap, characteristic, directory, archive);
            if(newDifficulty == null) continue;

            difficulties.Add(newDifficulty);
        }

        stopwatch.Stop();
        Debug.Log($"Difficulty loading took {stopwatch.ElapsedMilliseconds}ms.");
        return difficulties;
    }


//Suppress warnings about a lack of await when building for WebGL
#pragma warning disable 1998
    private static async Task<List<Difficulty>> LoadDifficultiesConcurrent(LoadedMapData mapData, string directory = null, ZipArchive archive = null)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new System.InvalidOperationException("Concurrent difficulty loading doesn't work in WebGL!");
#else
        System.Diagnostics.Stopwatch stopwatch = new();
        stopwatch.Start();

        Debug.Log("Loading difficulties concurrently.");
        MapLoader.LoadingMessage = "Loading difficulties";

        List<Task<Difficulty>> difficultyTasks = new List<Task<Difficulty>>();
        foreach(DifficultyBeatmap beatmap in mapData.Info.difficultyBeatmaps)
        {
            string characteristicName = beatmap.characteristic;

            DifficultyCharacteristic characteristic = BeatmapInfo.CharacteristicFromString(characteristicName);

            //Add each difficulty to a task list to run at once
            Debug.Log($"Adding {beatmap.characteristic}, {beatmap.difficulty} to task list.");

            if(!string.IsNullOrEmpty(directory))
            {
                //Loading map from directory
                Task<Difficulty> newDiffTask = Task.Run(() => LoadDifficulty(mapData, beatmap, characteristic, directory));
                difficultyTasks.Add(newDiffTask);
                continue;
            }

            //Loading map from zip file
            using Stream diffStream = archive?.GetEntryCaseInsensitive(beatmap.beatmapDataFilename)?.Open();
            if(diffStream != null)
            {
                //Read the byte array now because reading from the same ziparchive on multiple threads breaks shit
                byte[] diffData = FileUtil.StreamToBytes(diffStream);
                Task<Difficulty> newDiffTask = Task.Run(() => LoadDifficulty(mapData, beatmap, characteristic, null, diffData));
                difficultyTasks.Add(newDiffTask);
            }
            else
            {
                Debug.LogWarning($"Unable to get difficulty data from {beatmap.beatmapDataFilename}!");
            }
        }

        //Run all the difficulties are finished
        await Task.WhenAll(difficultyTasks.ToArray());

        List<Difficulty> difficulties = new List<Difficulty>();
        for(int i = 0; i < difficultyTasks.Count; i++)
        {
            Task<Difficulty> task = difficultyTasks[i];
            if(task.Result == null)
            {
                Debug.LogWarning($"Failed to load difficulty from task {i}!");
            }
            else difficulties.Add(task.Result);

            task.Dispose();
        }

        stopwatch.Stop();
        Debug.Log($"Difficulty loading took {stopwatch.ElapsedMilliseconds}ms.");
        return difficulties;
#endif
    }
#pragma warning restore 1998


    private static async Task<Difficulty> LoadDifficultyFile(LoadedMapData mapData, DifficultyBeatmap beatmap, DifficultyCharacteristic characteristic, string directory = null, ZipArchive archive = null)
    {
        if(!string.IsNullOrEmpty(directory))
        {
            return await LoadDifficulty(mapData, beatmap, characteristic, directory);
        }
        else if(archive != null)
        {
            using Stream diffStream = archive.GetEntryCaseInsensitive(beatmap.beatmapDataFilename)?.Open();
            byte[] diffData = FileUtil.StreamToBytes(diffStream);
            return await LoadDifficulty(mapData, beatmap, characteristic, null, diffData);
        }
        else
        {
            Debug.LogWarning($"Unable to load {beatmap.characteristic}, {beatmap.difficulty}!");
            return null;
        }
    }


    private static async Task<Difficulty> LoadDifficulty(LoadedMapData mapData, DifficultyBeatmap beatmap, DifficultyCharacteristic characteristic, string directory = null, byte[] diffData = null)
    {
        Difficulty difficulty;
        if(!string.IsNullOrEmpty(directory))
        {
            difficulty = await JsonReader.LoadDifficultyAsync(directory, beatmap, mapData);
        }
        else if(diffData != null && diffData.Length > 0)
        {
            difficulty = ZipReader.GetDifficulty(diffData, beatmap, mapData);
        }
        else difficulty = null;

        if(difficulty == null)
        {
            Debug.LogWarning($"Unable to load {beatmap.characteristic}, {beatmap.difficulty}!");
            return null;
        }

        difficulty.characteristic = characteristic;
        difficulty.environmentName = GetDifficultyEnvironmentName(mapData.Info, beatmap);
        difficulty.colorScheme = GetDifficultyColorScheme(mapData.Info, beatmap);

        difficulty.mappers = beatmap.beatmapAuthors.mappers ?? new string[0];
        difficulty.lighters = beatmap.beatmapAuthors.lighters ?? new string[0];

        FillCustomDifficultyData(ref difficulty, beatmap);
        return difficulty;
    }


    private static NullableColorPalette GetDifficultyColorScheme(BeatmapInfo info, DifficultyBeatmap beatmap)
    {
        BeatmapInfoColorScheme[] colorSchemes = info.colorSchemes;
        if(colorSchemes == null || colorSchemes.Length == 0)
        {
            //No color schemes are present
            return null;
        }

        int colorSchemeIndex = beatmap.beatmapColorSchemeIdx;
        if(colorSchemeIndex < 0 || colorSchemeIndex >= colorSchemes.Length)
        {
            //No color scheme of this index
            return null;
        }

        return colorSchemes[colorSchemeIndex].GetPalette();
    }


    private static string GetDifficultyEnvironmentName(BeatmapInfo info, DifficultyBeatmap beatmap)
    {
        string[] environmentNames = info.environmentNames;
        if(environmentNames == null || environmentNames.Length == 0)
        {
            return EnvironmentManager.V2Environments[0];
        }

        int environmentIndex = beatmap.environmentNameIdx;
        if(environmentIndex < 0 || environmentIndex >= environmentNames.Length)
        {
            //The environment list doesn't contain this index, so use default
            return EnvironmentManager.V2Environments[0];
        }

        return environmentNames[environmentIndex];
    }


    private static void FillCustomDifficultyData(ref Difficulty difficulty, DifficultyBeatmap beatmap)
    {
        difficulty.requirements = beatmap.customData?.requirements ?? new string[0];
        difficulty.label = beatmap.customData?.difficultyLabel ?? Difficulty.DiffLabelFromRank(difficulty.difficultyRank);
        difficulty.songCoreColors = ColorPaletteFromCustomData(beatmap.customData);
    }


    private static NullableColorPalette ColorPaletteFromCustomData(DifficultyBeatmapCustomData customData)
    {
        if(customData == null)
        {
            return null;
        }

        return new NullableColorPalette
        {
            LeftNoteColor = customData.colorLeft?.GetColor(),
            RightNoteColor = customData.colorRight?.GetColor(),
            LightColor1 = customData.envColorLeft?.GetColor(),
            LightColor2 = customData.envColorRight?.GetColor(),
            WhiteLightColor = customData.envColorWhite?.GetColor(),
            BoostLightColor1 = customData.envColorLeftBoost?.GetColor(),
            BoostLightColor2 = customData.envColorRightBoost?.GetColor(),
            BoostWhiteLightColor = customData.envColorWhiteBoost?.GetColor(),
            WallColor = customData.obstacleColor?.GetColor()
        };
    }
}