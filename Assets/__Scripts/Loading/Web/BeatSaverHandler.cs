using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class BeatSaverHandler
{
    private const string beatSaverApiURL = "https://api.beatsaver.com/";
    private const string mapDirect = "maps/id/";


    public static async Task<string> GetBeatSaverMapURL(string mapID)
    {
        string json = await GetApiResponse(mapID);

        if(json == "") return "";

        BeatSaverResponse response = JsonUtility.FromJson<BeatSaverResponse>(json);

        if(response.versions == null || response.versions.Length == 0)
        {
            return "";
        }

        return response.versions[0].downloadURL;
    }


    public static async Task<string> GetApiResponse(string mapID)
    {
        string url = string.Concat(beatSaverApiURL, mapDirect, mapID);

        try
        {
            using(UnityWebRequest uwr = UnityWebRequest.Get(url))
            {
                uwr.SendWebRequest();

                while(!uwr.isDone) await Task.Yield();

                if(uwr.result == UnityWebRequest.Result.Success)
                {
                    return uwr.downloadHandler.text;
                }
                else
                {
                    Debug.LogWarning(uwr.error);
                    ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find beatsaver map {mapID}! {uwr.error}");
                    return "";
                }
            }
        }
        catch(Exception err)
        {
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find beatsaver map {mapID}! {err.Message}");
            Debug.LogWarning($"Failed to get BeatSaver api response with error: {err.Message}, {err.StackTrace}");
            return "";
        }
    }
}


[Serializable] public struct BeatSaverResponse
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


[Serializable] public struct BeatSaverVersion
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


[Serializable] public struct BeatSaverDiff
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


[Serializable] public struct BeatSaverParitySummary
{
    public int errors;
    public int warns;
    public int resets;
}