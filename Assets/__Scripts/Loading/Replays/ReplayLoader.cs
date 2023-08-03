using System;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class ReplayLoader
{
    private const string beatleaderApiURL = "https://api.beatleader.xyz/";
    private const string scoreDirect = "score/";
    private const string userDirect = "player/";

//Suppress warnings about a lack of await when building for WebGL
#pragma warning disable 1998
    public static async Task<Replay> ReplayFromDirectory(string directory)
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        throw new InvalidOperationException("Loading from directory doesn't work in WebGL!");
#else
        try
        {
            byte[] replayData = await File.ReadAllBytesAsync(directory);
            return ReplayDecoder.Decode(replayData);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to load replay with error: {err.Message}, {err.StackTrace}");
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Failed to load replay file!");
            return null;
        }
#endif
    }


    public static async Task<Replay> ReplayFromStream(Stream replayStream)
    {
        AsyncReplayDecoder decoder = new AsyncReplayDecoder();
        (ReplayInfo, Task<Replay>) result = await decoder.StartDecodingStream(replayStream);

        Replay decodedReplay = await result.Item2;
        result.Item2.Dispose();
        return decodedReplay;
    }


    private static string DownloadURLFromResponse(string json)
    {
        if(string.IsNullOrEmpty(json))
        {
            return "";
        }

        try
        {
            BeatleaderScoreResponse score = JsonConvert.DeserializeObject<BeatleaderScoreResponse>(json);
            return score?.replay ?? "";
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to parse Beatleader response with error: {err.Message}, {err.StackTrace}");
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Failed to parse API response!");
            return "";
        }
    }


    public static async Task<string> ReplayURLFromScoreID(string scoreID)
    {
        string url = String.Concat(beatleaderApiURL, scoreDirect, scoreID);
#if UNITY_WEBGL && !UNITY_EDITOR
        // url = WebLoader.GetCorsURL(url);
#endif

        try
        {
            using UnityWebRequest uwr = UnityWebRequest.Get(url);
            uwr.SendWebRequest();

            while(!uwr.isDone) await Task.Yield();

            if(uwr.result == UnityWebRequest.Result.Success)
            {
                return DownloadURLFromResponse(uwr.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"Failed to get BeatLeader API response with error: {uwr.error}");
                ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find Beatleader score {scoreID}! {uwr.error}");
                return "";
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to get BeatLeader API response with error: {err.Message}, {err.StackTrace}");
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find Beatleader score {scoreID}! {err.Message}");
            return "";
        }
    }


    public static async Task<byte[]> AvatarDataFromBeatleaderUser(BeatleaderUser user)
    {
        string url = user.avatar;
        if(string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("Avatar url is empty!");
            return null;
        }
        Debug.Log($"Downloading avatar from {url}");

        try
        {
            using UnityWebRequest uwr = UnityWebRequest.Get(url);
            uwr.SendWebRequest();

            while(!uwr.isDone) await Task.Yield();

            if(uwr.result == UnityWebRequest.Result.Success)
            {
                return uwr.downloadHandler.data;
            }
            else
            {
                Debug.LogWarning($"Failed to get avatar image response with error: {uwr.error}");
                return null;
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to get avatar image response with error: {err.Message}, {err.StackTrace}");
            return null;
        }
    }


    public static async Task<BeatleaderUser> BeatleaderUserFromID(string userID)
    {
        string url = String.Concat(beatleaderApiURL, userDirect, userID);

        try
        {
            using UnityWebRequest uwr = UnityWebRequest.Get(url);
            uwr.SendWebRequest();

            while(!uwr.isDone) await Task.Yield();

            if(uwr.result == UnityWebRequest.Result.Success)
            {
                string json = uwr.downloadHandler.text;
                if(string.IsNullOrEmpty(json))
                {
                    Debug.LogWarning($"Beatleader user API response is empty!");
                    return null;
                }

                return JsonConvert.DeserializeObject<BeatleaderUser>(json);
            }
            else
            {
                Debug.LogWarning($"Failed to get BeatLeader user API response with error: {uwr.error}");
                return null;
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to get BeatLeader user API response with error: {err.Message}, {err.StackTrace}");
            return null;
        }
    }
}


[Serializable]
public class BeatleaderScoreResponse
{
    public int id;
    public int baseScore;
    public int modifiedScore;
    public float accuracy;
    public string playerId;
    public float pp;
    //wha tthe fuck beatleader why
    public float bonusPp;
    public float passPP;
    public float accPP;
    public float techPP;
    public int rank;
    public string country;
    public float fcAccuracy;
    public float fcPp;
    public float weight;
    public string replay;
    public string modifiers;
    //Ok I can't be assed to add all of this
}


[Serializable]
public class BeatleaderUser
{
    public string id;
    public string name;
    public string platform;
    public string avatar;
    public string country;
    public bool bot;
    public float pp;
    public int rank;
    public int countryRank;
    public string role;
}