using System;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS4014 //Suppress warnings about lack of await for uwr.SendWebRequest()
#pragma warning disable 1998
public class ReplayLoader
{
    private const string beatleaderApiURL = "https://api.beatleader.com/";
    private const string scoreDirect = "score/";
    private const string userDirect = "player/";
    private const string leaderboardDirect = "leaderboards/hash/";

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

        if(result.Item2 == null)
        {
            //Replay info decoding failed, the stream is likely not a replay file
            return null;
        }

        Replay decodedReplay = await result.Item2;
        result.Item2.Dispose();

        //Reset the stream back to the start so it can be reused properly
        replayStream.Seek(0, SeekOrigin.Begin);

        return decodedReplay;
    }


    private static BeatleaderScore ParseBeatleaderScore(string json)
    {
        if(string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            BeatleaderScore score = JsonConvert.DeserializeObject<BeatleaderScore>(json);
            return score;
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to parse BeatLeader response with error: {err.Message}, {err.StackTrace}");
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Failed to parse API response!");
            return null;
        }
    }


    public static async Task<BeatleaderScore> BeatleaderScoreFromID(string scoreID)
    {
        string url = String.Concat(beatleaderApiURL, scoreDirect, scoreID);
#if UNITY_WEBGL && !UNITY_EDITOR
        url = WebLoader.GetCorsURL(url);
#endif

        try
        {
            using UnityWebRequest uwr = UnityWebRequest.Get(url);
            uwr.SendWebRequest();

            while(!uwr.isDone) await Task.Yield();

            if(uwr.result == UnityWebRequest.Result.Success)
            {
                return ParseBeatleaderScore(uwr.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"Failed to get BeatLeader API response with error: {uwr.error}");
                ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find BeatLeader score {scoreID}! {uwr.error}");
                return null;
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to get BeatLeader API response with error: {err.Message}, {err.StackTrace}");
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find BeatLeader score {scoreID}! {err.Message}");
            return null;
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

#if UNITY_WEBGL && !UNITY_EDITOR
        url = WebLoader.GetCorsURL(url);
#endif

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
        string url = string.Concat(beatleaderApiURL, userDirect, userID);

#if UNITY_WEBGL && !UNITY_EDITOR
        url = WebLoader.GetCorsURL(url);
#endif

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
                    Debug.LogWarning($"BeatLeader user API response is empty!");
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


    public static string LeaderboardIDFromResponse(BeatleaderLeaderboardResponse response, string modeName, string difficultyName)
    {
        modeName = BeatmapInfo.TrimCharacteristicString(modeName);
        BeatleaderLeaderboard leaderboard = response.leaderboards.FirstOrDefault(x => x.difficulty.modeName == modeName && x.difficulty.difficultyName == difficultyName);
        if(leaderboard == null)
        {
            Debug.LogWarning($"Found no difficulty matching {modeName}, {difficultyName} in BeatLeader leaderboards!");
            return "";
        }
        return leaderboard.id;
    }


    public static async Task<BeatleaderLeaderboardResponse> LeaderboardFromHash(string hash)
    {
        string url = string.Concat(beatleaderApiURL, leaderboardDirect, hash);

#if UNITY_WEBGL && !UNITY_EDITOR
        url = WebLoader.GetCorsURL(url);
#endif

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
                    Debug.LogWarning($"BeatLeader leaderboard API response is empty!");
                    return null;
                }

                return JsonConvert.DeserializeObject<BeatleaderLeaderboardResponse>(json);
            }
            else
            {
                Debug.LogWarning($"Failed to get BeatLeader leaderboard API response with error: {uwr.error}");
                return null;
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to get BeatLeader leaderboard API response with error: {err.Message}, {err.StackTrace}");
            return null;
        }
    }
}


[Serializable]
public class BeatleaderScore
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

    public BeatleaderScoreSongData song;
}


[Serializable]
public class BeatleaderScoreSongData
{
    public string id;
    public string hash;
    public string cover;
    public string name;
    public string subName;
    public string author;
    public string mapper;
    public string downloadUrl;
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

    public BeatleaderUserProfileSettings profileSettings;
}


[Serializable]
public class BeatleaderUserProfileSettings
{
    public int id;
    public string leftSaberColor;
    public string rightSaberColor;
}


[Serializable]
public class BeatleaderLeaderboardResponse
{
    public BeatleaderSong song;
    public BeatleaderLeaderboard[] leaderboards;
}


[Serializable]
public class BeatleaderSong
{
    public string id;
    public string hash;
    public string name;
    public string subName;
    public string author;
    public string mapper;
    public string mapperId;
    public string coverImage;
    public string fullCoverImage;
    public string downloadUrl;
    public float bpm;
    public float duration;
    public string tags;
}


[Serializable]
public class BeatleaderLeaderboard
{
    public string id;
    public BeatleaderLeaderboardDifficulty difficulty;
}


[Serializable]
public class BeatleaderLeaderboardDifficulty
{
    public int id;
    public int value;
    public int mode;
    public string difficultyName;
    public string modeName;
    public string status;
}