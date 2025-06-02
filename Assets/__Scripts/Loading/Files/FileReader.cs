using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class FileReader : IMapDataLoader
{
    public string MapPath;


    public FileReader(string directory)
    {
        MapPath = directory;
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

        string audioDirectory = Path.Combine(MapPath, info.audio.songFilename);
        AudioClip song = await AudioFileHandler.LoadAudioDirectory(audioDirectory);
        if(song == null)
        {
            //Failed to load song
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load song file!");
            return LoadedMap.Empty;
        }

        Debug.Log("Loading cover image.");
        MapLoader.LoadingMessage = "Loading cover image";

        string coverImageDirectory = Path.Combine(MapPath, info.coverImageFilename);
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
        BeatmapInfo info = await Task.Run(() => JsonReader.LoadInfoAsync(MapPath));

        if(info == null)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Unable to load Info.dat!");
            return LoadedMapData.Empty;
        }

        LoadedMapData mapData = new LoadedMapData(info)
        {
            BpmEvents = await JsonReader.GetBpmEventsAsync(info, MapPath),
            Lightshows = await LightshowLoader.GetLightshowsAsync(info, MapPath)
        };
        mapData.Difficulties = await Task.Run(() => DifficultyLoader.GetDifficultiesAsync(mapData, MapPath));

        try
        {
            await LoadBookmarks(mapData);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to load bookmarks with error: {err.Message}, {err.StackTrace}");
        }

        return mapData;
#endif
    }


    private async Task LoadBookmarks(LoadedMapData mapData)
    {
        const string bookmarkFolder = "Bookmarks";
        const string bookmarkExtension = ".bookmarks.dat";

        List<BeatmapBookmarkSet> bookmarkSets = new List<BeatmapBookmarkSet>();

        string bookmarkDirectory = Path.Combine(MapPath, bookmarkFolder);
        if(!Directory.Exists(bookmarkDirectory))
        {
            //No official bookmarks to load
            BookmarkLoader.ApplyBookmarks(mapData, bookmarkSets);
            return;
        }

        foreach(string file in Directory.EnumerateFiles(bookmarkDirectory))
        {
            if(!file.EndsWith(bookmarkExtension, StringComparison.InvariantCultureIgnoreCase))
            {
                //This is not a valid bookmark file
                continue;
            }

            BeatmapBookmarkSet bookmarkSet = await BookmarkSetFromPath(file);
            if(bookmarkSet != null)
            {
                bookmarkSets.Add(bookmarkSet);
            }
        }

        BookmarkLoader.ApplyBookmarks(mapData, bookmarkSets);
    }


    private async Task<BeatmapBookmarkSet> BookmarkSetFromPath(string path)
    {
        try
        {
            string bookmarkJson = await JsonReader.ReadFileAsync(path);
            if(string.IsNullOrEmpty(bookmarkJson))
            {
                //Loading the text from file failed
                return null;
            }

            return JsonReader.DeserializeObject<BeatmapBookmarkSet>(bookmarkJson);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to read {Path.GetFileName(path)} with error: {err.Message}, {err.StackTrace}");
            return null;
        }
    }


    public void Dispose()
    {
        //Nothing to dispose here
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