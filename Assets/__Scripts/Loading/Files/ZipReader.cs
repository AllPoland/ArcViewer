using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

public class ZipReader
{
    public static async Task<LoadedMap> MapFromZipArchiveAsync(ZipArchive archive)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        LoadedMapData mapData = await Task.Run(() => MapDataFromZipArchiveAsync(archive));
#else
        LoadedMapData mapData = await MapDataFromZipArchiveAsync(archive);
#endif
        if(mapData.Info == null)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load Info.dat!");
            return LoadedMap.Empty;
        }
        if(mapData.Difficulties == null || mapData.Difficulties.Count == 0)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load difficulties!");
            return LoadedMap.Empty;
        }

        BeatmapInfo info = mapData.Info;

        MemoryStream songData = null;
        string songFilename = info?._songFilename ?? "";
        foreach(ZipArchiveEntry entry in archive.Entries)
        {
            if(entry.Name.Equals(songFilename))
            {
                //We need to convert the stream specifically to a Memory Stream to make it seekable
                //This is required for audio processing to work
                using(Stream songStream = entry.Open())
                {
                    songData = new MemoryStream(FileUtil.StreamToBytes(songStream));
                }
                break;
            }
        }
        if(songData == null)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Song file not found!");
            Debug.LogWarning($"Didn't find audio file {songFilename}!");
            return LoadedMap.Empty;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        AudioClip song = await AudioClipFromMemoryStream(songData, songFilename);
#else
        WebAudioClip song = await AudioFileHandler.WebAudioClipFromStream(songData);
#endif
        if(song == null)
        {
            //Failed to load song
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load song file!");
            return LoadedMap.Empty;
        }

        MapLoader.LoadingMessage = "Loading cover image";
        Debug.Log("Loading cover image.");
        await Task.Yield();

        byte[] coverImageData = new byte[0];
        string coverFilename = info?._coverImageFilename ?? "";
        foreach(ZipArchiveEntry entry in archive.Entries)
        {
            if(entry.Name.Equals(coverFilename))
            {
                using(Stream coverImageStream = entry.Open())
                {
                    coverImageData = FileUtil.StreamToBytes(coverImageStream);
                }
            }
        }
        if(coverImageData == null || coverImageData.Length <= 0)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, "Cover image not found!");
            Debug.Log($"Didn't find image file {coverFilename}!");
        }

        return new LoadedMap(mapData, coverImageData, song);
    }


    public static async Task<LoadedMapData> MapDataFromZipArchiveAsync(ZipArchive archive)
    {
        BeatmapInfo info = null;

        MapLoader.LoadingMessage = "Loading Info.dat";
        await Task.Yield();
        info = GetInfoData(archive);

        if(info == null)
        {
            //Failed to load info (errors are handled in GetInfoData)
            return LoadedMapData.Empty;
        }

        Debug.Log($"Loaded info for {info._songAuthorName} - {info._songName}, mapped by {info._levelAuthorName}");

        if(info._difficultyBeatmapSets == null || info._difficultyBeatmapSets.Length < 1)
        {
            Debug.LogWarning("Info lists no difficulty sets!");
            return LoadedMapData.Empty;
        }
        List<Difficulty> difficulties = await MapLoader.GetDifficultiesAsync(info, archive);

        return new LoadedMapData(info, difficulties);
    }


    public static BeatmapInfo GetInfoData(ZipArchive archive)
    {
        Stream infoData = null;

        foreach(ZipArchiveEntry entry in archive.Entries)
        {
            if(!entry.Name.Equals("Info.dat", System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                infoData = entry.Open();
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to read Info.dat with error: {e.Message}, {e.StackTrace}");
                return null;
            }

            if(!entry.FullName.Equals("Info.dat", System.StringComparison.OrdinalIgnoreCase))
            {
                //This means Info.dat was in a subfolder
                ErrorHandler.Instance.QueuePopup(ErrorType.Warning, "Map files aren't in the root!");
                Debug.LogWarning("Map files aren't in the root!");
            }
            break;
        }

        if(infoData == null)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load Info.dat!");
            Debug.LogWarning("Unable to load Info.dat!");

            return null;
        }

        string infoJson = System.Text.Encoding.UTF8.GetString(FileUtil.StreamToBytes(infoData));
        try
        {
            return JsonUtility.FromJson<BeatmapInfo>(infoJson);
        }
        catch(Exception e)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to parse Info.dat!");
            Debug.LogWarning($"Failed to parse Info.dat with error: {e.Message}, {e.StackTrace}");

            return null;
        }
    }


    public static Difficulty GetDifficulty(ZipArchive archive, DifficultyBeatmap beatmap)
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

        Stream diffData = null;
        foreach(ZipArchiveEntry entry in archive.Entries)
        {
            if(entry.Name.Equals(filename))
            {
                diffData = entry.Open();
                break;
            }
        }

        if(diffData == null)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, $"Unable to load {filename}!");
            Debug.LogWarning("Difficulty file not found!");
            return null;
        }

        Debug.Log($"Parsing {filename}");
        string diffJson = System.Text.Encoding.UTF8.GetString(FileUtil.StreamToBytes(diffData));
        output.beatmapDifficulty = JsonReader.ParseBeatmapFromJson(diffJson);
        diffData.Dispose();

        return output;
    }


#if !UNITY_WEBGL || UNITY_EDITOR
    private static async Task<AudioClip> AudioClipFromMemoryStream(MemoryStream stream, string songFilename)
    {
        //Write song data to a tempfile so it can be loaded through a uwr
        using TempFile songFile = new TempFile();
        await File.WriteAllBytesAsync(songFile.Path, stream.ToArray());

        return await AudioFileHandler.LoadAudioDirectory(songFile.Path, songFilename);
    }
#endif
}