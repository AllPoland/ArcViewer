using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class ZipReader : IMapDataLoader
{
    public ZipArchive Archive;
    public Stream ArchiveStream;


    public ZipReader(ZipArchive archive, Stream archiveStream = null)
    {
        Archive = archive;
        ArchiveStream = archiveStream;
    }


    public ZipReader()
    {
        Archive = null;
        ArchiveStream = null;
    }


    public async Task<LoadedMap> GetMap()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        LoadedMapData mapData = await Task.Run(() => GetMapData());
#else
        LoadedMapData mapData = await GetMapData();
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

        Debug.Log("Loading audio file.");
        MapLoader.LoadingMessage = "Loading song";
        await Task.Yield();

        string songFilename = info.audio?.songFilename ?? "";
        using Stream songStream = Archive.GetEntryCaseInsensitive(songFilename)?.Open();
        if(songStream == null)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Song file not found!");
            Debug.LogWarning($"Didn't find audio file {songFilename}!");
            return LoadedMap.Empty;
        }

        using MemoryStream songData = new MemoryStream(FileUtil.StreamToBytes(songStream));
        if(songData == null)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to read song file!");
            Debug.LogWarning($"Failed to read memory stream for audio file {songFilename}!");
            return LoadedMap.Empty;
        }

#if !UNITY_WEBGL || UNITY_EDITOR
        AudioClip song = await AudioClipFromMemoryStream(songData, songFilename);
#else
        WebAudioClip song = await AudioFileHandler.WebAudioClipFromStream(songData, songFilename);
#endif
        if(song == null)
        {
            //Failed to load song
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load song file!");
            return LoadedMap.Empty;
        }

        Debug.Log("Loading cover image.");
        MapLoader.LoadingMessage = "Loading cover image";
        await Task.Yield();

        byte[] coverImageData = new byte[0];
        string coverFilename = info.coverImageFilename ?? "";
        using Stream coverImageStream = Archive.GetEntryCaseInsensitive(coverFilename)?.Open();
        if(coverImageStream == null)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, "Cover image not found!");
            Debug.Log($"Didn't find image file {coverFilename}!");
        }
        else
        {
            coverImageData = FileUtil.StreamToBytes(coverImageStream);
            if(coverImageData == null || coverImageData.Length <= 0)
            {
                ErrorHandler.Instance.QueuePopup(ErrorType.Warning, "Failed to read cover image data!");
                Debug.Log($"Failed to read bytes from {coverFilename}!");
            }
        }

        return new LoadedMap(mapData, coverImageData, song);
    }


    public async Task<LoadedMapData> GetMapData()
    {
        BeatmapInfo info = null;

        MapLoader.LoadingMessage = "Loading Info.dat";
        await Task.Yield();
        info = GetInfoData(Archive);

        if(info == null)
        {
            //Failed to load info (errors are handled in GetInfoData)
            return LoadedMapData.Empty;
        }

        Debug.Log($"Loaded info for {info.song.author} - {info.song.title}");

        if(info.difficultyBeatmaps == null || info.difficultyBeatmaps.Length < 1)
        {
            Debug.LogWarning("Info lists no difficulties!");
            return LoadedMapData.Empty;
        }
        List<Difficulty> difficulties = await DifficultyLoader.GetDifficultiesAsync(info, Archive);

        return new LoadedMapData(info, difficulties);
    }


    public void Dispose()
    {
        Archive?.Dispose();
        ArchiveStream?.Dispose();
    }


    private static BeatmapInfo GetInfoData(ZipArchive archive)
    {
        Stream infoData = null;

        foreach(ZipArchiveEntry entry in archive.Entries)
        {
            if(!entry.FullName.Equals("Info.dat", StringComparison.InvariantCultureIgnoreCase))
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
            break;
        }

        if(infoData == null)
        {
            //Search subfolders for Info
            foreach(ZipArchiveEntry entry in archive.Entries)
            {
                if(!entry.Name.Equals("Info.dat", StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                //Info.dat was found in a subfolder
                ErrorHandler.Instance.QueuePopup(ErrorType.Warning, "Map files aren't in the root!");
                Debug.LogWarning("Map files aren't in the root!");
                try
                {
                    infoData = entry.Open();
                }
                catch(Exception e)
                {
                    Debug.LogWarning($"Failed to read Info.dat with error: {e.Message}, {e.StackTrace}");
                    return null;
                }
                break;
            }
        }

        if(infoData == null)
        {
            //Info is still not found
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load Info.dat!");
            Debug.LogWarning("Unable to load Info.dat!");

            return null;
        }

        string infoJson = System.Text.Encoding.UTF8.GetString(FileUtil.StreamToBytes(infoData));
        BeatmapInfo info = JsonReader.ParseInfoFromJson(infoJson);

        if(info == null)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to parse Info.dat!");
        }

        return info;
    }


    public static Difficulty GetDifficulty(byte[] diffData, DifficultyBeatmap beatmap)
    {
        Difficulty output = new Difficulty
        {
            difficultyRank = MapLoader.DiffValueFromString[beatmap.difficulty],
            noteJumpSpeed = beatmap.noteJumpMovementSpeed,
            spawnOffset = beatmap.noteJumpStartBeatOffset
        };

        string diffJson = System.Text.Encoding.UTF8.GetString(diffData);
        output.beatmapDifficulty = JsonReader.ParseBeatmapFromJson(diffJson, beatmap.beatmapDataFilename);

        return output;
    }


#if !UNITY_WEBGL || UNITY_EDITOR
    private static async Task<AudioClip> AudioClipFromMemoryStream(MemoryStream stream, string songFilename)
    {
        //Write song data to a tempfile so it can be loaded through a uwr
        Debug.Log($"Writing {songFilename} to temp file.");
        using TempFile songFile = new TempFile();
        await File.WriteAllBytesAsync(songFile.Path, stream.ToArray());

        return await AudioFileHandler.LoadAudioDirectory(songFile.Path, songFilename);
    }
#endif
}