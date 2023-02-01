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

    public static int MaxCacheSize = 3;

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

        while(CachedFiles.Count >= MaxCacheSize)
        {
            RemoveLastFile();
        }

        string randomString = RandomStuff.RandomString(10);
        string newFilePath = Path.Combine(CachePath, $"{randomString}.zip");
        CachedFile newFile = new CachedFile
        {
            Key = key,
            FilePath = newFilePath
        };

        File.WriteAllBytes(newFilePath, ZipReader.StreamToBytes(fileStream));
        CachedFiles.Add(newFile);

        SaveCacheData();

        Debug.Log($"Successfully saved {randomString}.zip to cache.");
    }


    private static void RemoveLastFile()
    {
        if(CachedFiles == null || CachedFiles.Count <= 0)
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
    }


    private static void SaveCacheData()
    {
        if(CachedFiles == null || CachedFiles.Count <= 0)
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