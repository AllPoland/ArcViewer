using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public class FileReader : MonoBehaviour
{
    public static async Task<LoadedMap> LoadMapDirectoryAsync(string directory)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new System.NotImplementedException("Loading from directory doesn't work in WebGL!");
#else
        LoadedMapData mapData = await LoadMapDataDirectoryAsync(directory);
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

        MapLoader.LoadingMessage = "Loading song.";
        string audioDirectory = Path.Combine(directory, info._songFilename);
        AudioClip song = await AudioFileHandler.LoadAudioDirectory(audioDirectory);
        if(song == null)
        {
            //Failed to load song
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Unable to load song file!");
            return LoadedMap.Empty;
        }

        Debug.Log("Loading cover image.");
        MapLoader.LoadingMessage = "Loading cover image.";

        string coverImageDirectory = Path.Combine(directory, info._coverImageFilename);
        byte[] coverImageData = await Task.Run(() => LoadCoverImageData(coverImageDirectory));

        return new LoadedMap(mapData, coverImageData, song);
#endif
    }


    public static async Task<LoadedMapData> LoadMapDataDirectoryAsync(string directory)
    {
        Debug.Log("Loading info.");
        MapLoader.LoadingMessage = "Loading Info.dat";
        BeatmapInfo info = await Task.Run(() => LoadInfoAsync(directory));

        if(info == null)
        {
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Unable to load Info.dat!");
            return LoadedMapData.Empty;
        }

        Debug.Log($"Loaded info for {info._songAuthorName} - {info._songName}, mapped by {info._levelAuthorName}");

        Debug.Log("Loading difficulties.");
        List<Difficulty> difficulties = await Task.Run(() => MapLoader.GetDifficultiesAsync(info, directory));

        return new LoadedMapData(info, difficulties);
    }


    private static async Task<BeatmapInfo> LoadInfoAsync(string directory)
    {
        string infoPath = Path.Combine(directory, "Info.dat");

        BeatmapInfo info = await JsonReader.LoadInfoAsync(infoPath);
        if(info == null) return null;

        Debug.Log($"Loaded info for {info._songAuthorName} - {info._songName}, mapped by {info._levelAuthorName}");
        return info;
    }


    private static byte[] LoadCoverImageData(string directory)
    {
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
    }
}