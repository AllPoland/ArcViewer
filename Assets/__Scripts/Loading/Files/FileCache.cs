using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class FileCache
{
    public const string CacheFolder = "Cache/";
    public const string CacheJson = "CacheInfo.json";
    public static readonly string CachePath = Path.Combine(Application.persistentDataPath, CacheFolder);
    public static readonly string CacheDataFile = Path.Combine(CachePath, CacheJson);

    private static int _maxCacheSize = 3;
    public static int MaxCacheSize
    {
        get => _maxCacheSize;
        set
        {
            _maxCacheSize = value;
            if(CachedFiles != null && CachedFiles.Count > value)
            {
                while(CachedFiles.Count > value)
                {
                    RemoveLastFile();
                }
                SaveCacheData();
            }
        }
    }

    private static List<CachedFile> CachedFiles;


    public static string GetCachedFile(string key)
    {
        if(CachedFiles == null)
        {
            LoadCachedFiles();
        }

        if(CachedFiles.Count <= 0)
        {
            return "";
        }

        CachedFile match = CachedFiles.FirstOrDefault(x => x.Key == key);
        return match?.FilePath ?? "";
    }


    public static void SaveFileToCache(Stream fileStream, string key)
    {
        if(CachedFiles == null)
        {
            LoadCachedFiles();
        }

        if(CachedFiles.Any(x => x.Key == key))
        {
            //This file has already been cached
            return;
        }

        if(MaxCacheSize == 0)
        {
            return;
        }

        if(CachedFiles.Count >= MaxCacheSize)
        {
            RemoveLastFile();
        }

        string randomString = RandomStuff.RandomString(10);
        string newFilePath = Path.Combine(CachePath, $"{randomString}.zip");
        while(File.Exists(newFilePath))
        {
            //Regenerate a new name if a file with this name already exists
            //The chances of this happening are astronomically low, but just to be safe smil
            randomString = RandomStuff.RandomString(10);
            newFilePath = Path.Combine(CachePath, $"{randomString}.zip");
        }

        CachedFile newFile = new CachedFile
        {
            Key = key,
            FilePath = newFilePath
        };

        File.WriteAllBytes(newFilePath, FileUtil.StreamToBytes(fileStream));
        CachedFiles.Add(newFile);

        SaveCacheData();

        Debug.Log($"Successfully saved {randomString}.zip to cache.");
    }


    private static void RemoveLastFile()
    {
        if(CachedFiles == null)
        {
            LoadCachedFiles();
        }

        if(CachedFiles.Count <= 0)
        {
            return;
        }

        CachedFile toRemove = CachedFiles[0];

        if(File.Exists(toRemove.FilePath))
        {
            File.Delete(toRemove.FilePath);
        }

        CachedFiles.Remove(toRemove);
    }


    private static void LoadCachedFiles()
    {
        if(!Directory.Exists(CachePath))
        {
            Directory.CreateDirectory(CachePath);
        }

        if(!File.Exists(CacheDataFile))
        {
            CachedFiles = new List<CachedFile>();
            return;
        }

        string json = File.ReadAllText(CacheDataFile);
        CachedFiles = JsonConvert.DeserializeObject<List<CachedFile>>(json);

        while(CachedFiles.Count > MaxCacheSize)
        {
            RemoveLastFile();
        }
        SaveCacheData();
    }


    private static void SaveCacheData()
    {
        if(CachedFiles == null)
        {
            return;
        }

        string json = JsonConvert.SerializeObject(CachedFiles);
        File.WriteAllText(CacheDataFile, json);
    }


    [Serializable]
    public class CachedFile
    {
        public string Key;
        public string FilePath;
    }
}