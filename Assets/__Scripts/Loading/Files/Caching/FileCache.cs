using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

public class FileCache
{
    public string CacheJson = "CacheInfo.json";
    public string FileExtension = ".zip";

    private int _maxCacheSize = 5;
    public int MaxCacheSize
    {
        get => _maxCacheSize;
        set
        {
            _maxCacheSize = Mathf.Max(value, 0);

            if(CachedFiles == null)
            {
                LoadCachedFiles();
            }

            while(CachedFiles.Count > _maxCacheSize)
            {
                RemoveLastFile();
            }
            SaveCacheData();
        }
    }

    private string CachePath => CacheManager.CachePath;
    private string CacheDataFile => Path.Combine(CachePath, CacheJson);

    private List<CachedFile> CachedFiles;


    public FileCache(string jsonFilename, string fileExtension)
    {
        CacheJson = jsonFilename ?? CacheJson;
        FileExtension = fileExtension ?? FileExtension;
    }


    public CachedFile GetCachedFile(string url = null, string id = null, string hash = null)
    {
        if(CachedFiles == null)
        {
            LoadCachedFiles();
        }

        if(CachedFiles.Count <= 0)
        {
            return null;
        }

        CachedFile match = CachedFiles.FirstOrDefault(x => x.MatchInput(url, id, hash));
        if(match != null)
        {
            //Move this file to the end of the list
            CachedFiles.Remove(match);
            CachedFiles.Add(match);

            //Update fields if we have new ones
            if(!string.IsNullOrEmpty(url)) match.URL = url;
            if(!string.IsNullOrEmpty(id)) match.ID = id;
            if(!string.IsNullOrEmpty(hash)) match.Hash = hash;

            SaveCacheData();
        }

        return match;
    }


    public void SaveFileToCache(Stream fileStream, string url = null, string id = null, string hash = null)
    {
        if(CachedFiles == null)
        {
            LoadCachedFiles();
        }

        CachedFile match = CachedFiles.FirstOrDefault(x => x.MatchInput(url, id, hash));
        if(match != null)
        {
            //This file has already been cached, update fields if we have new ones
            if(!string.IsNullOrEmpty(url)) match.URL = url;
            if(!string.IsNullOrEmpty(id)) match.ID = id;
            if(!string.IsNullOrEmpty(hash)) match.Hash = hash;

            SaveCacheData();
            Debug.Log("Tried to save an already cached file.");
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
        string newFilePath = Path.Combine(CachePath, $"{randomString}{FileExtension}");
        while(File.Exists(newFilePath))
        {
            //Regenerate a new name if a file with this name already exists
            //The chances of this happening are astronomically low, but just to be safe smil
            randomString = RandomStuff.RandomString(10);
            newFilePath = Path.Combine(CachePath, $"{randomString}{FileExtension}");
        }

        CachedFile newFile = new CachedFile
        {
            URL = url,
            ID = id,
            Hash = hash,
            FilePath = newFilePath
        };

        try
        {
            File.WriteAllBytes(newFilePath, FileUtil.StreamToBytes(fileStream));
            CachedFiles.Add(newFile);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to write map zip data with error: {e.Message}, {e.StackTrace}");
        }

        SaveCacheData();

        Debug.Log($"Successfully saved {randomString}{FileExtension} to cache.");
    }


    public void Clear()
    {
        CachedFiles = null;
    }


    private void RemoveLastFile()
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
            try
            {
                File.Delete(toRemove.FilePath);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to delete {toRemove.FilePath} with error: {e.Message}, {e.StackTrace}");
            }
        }

        CachedFiles.Remove(toRemove);
    }


    private void LoadCachedFiles()
    {
        if(!Directory.Exists(CachePath))
        {
            Debug.Log($"Directory {CachePath} doesn't exist, making it.");
            try
            {
                Directory.CreateDirectory(CachePath);
            }
            catch(Exception e)
            {
                Debug.LogWarning($"Failed to create cache directory with error: {e.Message}, {e.StackTrace}");
                CachedFiles = new List<CachedFile>();
                return;
            }
        }

        if(!File.Exists(CacheDataFile))
        {
            CachedFiles = new List<CachedFile>();
            return;
        }

        try
        {
            string json = File.ReadAllText(CacheDataFile);
            CachedFiles = JsonConvert.DeserializeObject<List<CachedFile>>(json);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to read cache data with error: {e.Message}, {e.StackTrace}");
            CachedFiles = new List<CachedFile>();
        }

        while(CachedFiles.Count > MaxCacheSize)
        {
            RemoveLastFile();
        }
        SaveCacheData();
    }


    private void SaveCacheData()
    {
        if(CachedFiles == null)
        {
            return;
        }

        try
        {
            string json = JsonConvert.SerializeObject(CachedFiles);
            File.WriteAllText(CacheDataFile, json);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to write cache data with error: {e.Message}, {e.StackTrace}");
        }
    }
}


[Serializable]
public class CachedFile
{
    public string URL;
    public string ID;
    public string Hash;
    public string FilePath;

    public bool MatchInput(string url, string id, string hash)
    {
        if(!string.IsNullOrEmpty(url) && url == URL)
        {
            return true;
        }
        else if(!string.IsNullOrEmpty(id) && id == ID)
        {
            return true;
        }
        else if(!string.IsNullOrEmpty(hash) && hash == Hash)
        {
            return true;
        }
        else return false;
    }
}