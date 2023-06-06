using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class FileReader : IMapDataLoader
{
    public string Directory;


    public FileReader(string directory)
    {
        Directory = directory;
    }

//Suppress warnings about a lack of await when building for WebGL
#pragma warning disable 1998
    public async Task<LoadedMap> GetMap()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new System.InvalidOperationException("Loading from directory doesn't work in WebGL!");
#else
        LoadedMapData mapData = await GetMapData();
        if(mapData.Info == null)
        {
            //Failed to load info
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load Info.dat!");
            return LoadedMap.Empty;
        }
        if(mapData.Difficulties == null || mapData.Difficulties.Count == 0)
        {
            //Failed to load difficulties
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load difficulties!");
            return LoadedMap.Empty;
        }

        BeatmapInfo info = mapData.Info;

        Debug.Log("Loading audio file.");
        MapLoader.LoadingMessage = "Loading song";

        string audioDirectory = Path.Combine(Directory, info._songFilename);
        AudioClip song = await AudioFileHandler.LoadAudioDirectory(audioDirectory);
        if(song == null)
        {
            //Failed to load song
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load song file!");
            return LoadedMap.Empty;
        }

        Debug.Log("Loading cover image.");
        MapLoader.LoadingMessage = "Loading cover image";

        string coverImageDirectory = Path.Combine(Directory, info._coverImageFilename);
        byte[] coverImageData = await Task.Run(() => LoadCoverImageData(coverImageDirectory));

        return new LoadedMap(mapData, coverImageData, song);
#endif
    }


    public async Task<LoadedMapData> GetMapData()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new System.InvalidOperationException("Loading from directory doesn't work in WebGL!");
#else
        Debug.Log("Loading info.");
        MapLoader.LoadingMessage = "Loading Info.dat";
        BeatmapInfo info = await Task.Run(() => LoadInfoAsync(Directory));

        if(info == null)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Unable to load Info.dat!");
            return LoadedMapData.Empty;
        }

        Debug.Log("Loading difficulties.");
        List<Difficulty> difficulties = await Task.Run(() => DifficultyLoader.GetDifficultiesAsync(info, Directory));

        return new LoadedMapData(info, difficulties);
#endif
    }


    public void Dispose()
    {
        //Nothing to dispose here
    }


    private static async Task<BeatmapInfo> LoadInfoAsync(string directory)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new System.InvalidOperationException("Loading from directory doesn't work in WebGL!");
#else
        string infoPath = Path.Combine(directory, "Info.dat");

        BeatmapInfo info = await JsonReader.LoadInfoAsync(infoPath);
        if(info == null) return null;

        Debug.Log($"Loaded info for {info._songAuthorName} - {info._songName}, mapped by {info._levelAuthorName}");
        return info;
#endif
    }


    private static byte[] LoadCoverImageData(string directory)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new System.InvalidOperationException("Loading from directory doesn't work in WebGL!");
#else
        using Stream coverImageStream = FileUtil.ReadFileData(directory);
        if(coverImageStream != null)
        {
            return FileUtil.StreamToBytes(coverImageStream);
        }
        else
        {
            Debug.LogWarning("Failed to load cover image!");
            ErrorHandler.Instance.QueuePopup(ErrorType.Warning, "Cover image not found!");
            return null;
        }
#endif
    }
}