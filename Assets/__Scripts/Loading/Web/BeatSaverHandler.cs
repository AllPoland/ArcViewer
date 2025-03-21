using System;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS4014 //Suppress warnings about lack of await for uwr.SendWebRequest()
public static class BeatSaverHandler
{
    public static readonly string[] BeatSaverCdnURLs = new string[]
    {
        "r2cdn.beatsaver.com",
        "cdn.beatsaver.com"
    };

    private const string beatSaverApiURL = "https://api.beatsaver.com/";
    private const string idDirect = "maps/id/";
    private const string hashDirect = "maps/hash/";


    public static async Task<(string[], string)> GetBeatSaverMapHash(string hash)
    {
        string json = await GetApiResponse(hashDirect, hash, false);

        if(string.IsNullOrEmpty(json)) return (null, "");

        BeatSaverResponse response = JsonUtility.FromJson<BeatSaverResponse>(json);

        if(response.versions == null || response.versions.Length == 0)
        {
            return (null, "");
        }

        string url = response.versions.FirstOrDefault(x => x.hash.Equals(hash, StringComparison.InvariantCultureIgnoreCase))?.downloadURL;
        if(string.IsNullOrEmpty(url))
        {
            Debug.Log("BeatSaver response doesn't contain this outdated version!");

            //Return an array of potential urls for this map
            string[] urls = new string[BeatSaverCdnURLs.Length + 1];
            for(int i = 0; i < BeatSaverCdnURLs.Length; i++)
            {
                urls[i] = $"https://{BeatSaverCdnURLs[i]}/{hash.ToLower()}.zip";
            }
            //End with the url for the latest map version as a fallback
            urls[BeatSaverCdnURLs.Length] = url;

            return (urls, response.id);
        }
        else return (new string[] {url}, response.id);
    }


    public static async Task<string> GetBeatSaverMapID(string mapID)
    {
        string json = await GetApiResponse(idDirect, mapID, true);

        if(string.IsNullOrEmpty(json)) return "";

        BeatSaverResponse response = JsonUtility.FromJson<BeatSaverResponse>(json);

        if(response.versions == null || response.versions.Length == 0)
        {
            return "";
        }

        return response.versions[0].downloadURL;
    }


    private static async Task<string> GetApiResponse(string apiDirect, string mapID, bool showError)
    {
        string url = string.Concat(beatSaverApiURL, apiDirect, mapID);

        try
        {
            using UnityWebRequest uwr = UnityWebRequest.Get(url);
            uwr.SendWebRequest();

            while(!uwr.isDone) await Task.Yield();

            if(uwr.result == UnityWebRequest.Result.Success)
            {
                return uwr.downloadHandler.text;
            }
            else
            {
                if(showError)
                {
                    ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find BeatSaver map {mapID}! {uwr.error}");
                }

                Debug.LogWarning(uwr.error);
                return "";
            }
        }
        catch(Exception err)
        {
            if(showError)
            {
                ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find BeatSaver map {mapID}! {err.Message}");
            }

            Debug.LogWarning($"Failed to get BeatSaver api response with error: {err.Message}, {err.StackTrace}");
            return "";
        }
    }
}


[Serializable]
public class BeatSaverResponse
{
    public string id;
    public string name;
    public string description;
    public DateTime uploaded;
    public bool automapper;
    public bool ranked;
    public bool qualified;

    public BeatSaverVersion[] versions;
}


[Serializable]
public class BeatSaverVersion
{
    public string hash;
    public string state;
    public DateTime createdAt;
    public int sageScore;
    
    public BeatSaverDiff[] diffs;

    public string downloadURL;
    public string coverURL;
    public string previewURL;
}


[Serializable]
public class BeatSaverDiff
{
    public float njs;
    public float offset;
    public ulong notes;
    public ulong bombs;
    public ulong obstacles;
    public float nps;
    public float length;
    public string characteristic;
    public string difficulty;
    public ulong events;
    public bool me;
    public bool ne;
    public bool cinema;
    public float seconds;
    public BeatSaverParitySummary paritySummary;
    public ulong maxScore;
    public string label;
}


[Serializable]
public class BeatSaverParitySummary
{
    public int errors;
    public int warns;
    public int resets;
}