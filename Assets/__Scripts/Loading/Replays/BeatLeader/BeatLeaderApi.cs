using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS4014
#pragma warning disable 1998
public static class BeatLeaderApi
{
    private const string ScoreDirect = "score/";
    private const string UserDirect = "player/";
    private const string LeaderboardDirect = "leaderboards/hash/";


    public static async Task<BeatLeaderScore> ScoreFromID(string scoreID, bool showErrors = true)
    {
        string url = string.Concat(ReplaySources.BeatLeader.ApiURL, ScoreDirect, scoreID);
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
                return ParseScore(uwr.downloadHandler.text);
            }
            else
            {
                Debug.LogWarning($"Failed to get BeatLeader API response with error: {uwr.error}");
                if(showErrors)
                {
                    ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find BeatLeader score {scoreID}! {uwr.error}");
                }
                return null;
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to get BeatLeader API response with error: {err.Message}, {err.StackTrace}");
            if(showErrors)
            {
                ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find BeatLeader score {scoreID}! {err.Message}");
            }
            return null;
        }
    }


    public static async Task<BeatLeaderUser> UserFromID(string userID)
    {
        string url = string.Concat(ReplaySources.BeatLeader.ApiURL, UserDirect, userID);

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

                return JsonConvert.DeserializeObject<BeatLeaderUser>(json);
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


    public static async Task<BeatLeaderLeaderboardResponse> LeaderboardFromHash(string hash)
    {
        string url = string.Concat(ReplaySources.BeatLeader.ApiURL, LeaderboardDirect, hash);

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

                return JsonConvert.DeserializeObject<BeatLeaderLeaderboardResponse>(json);
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


    public static string LeaderboardIDFromResponse(BeatLeaderLeaderboardResponse response, string modeName, string difficultyName)
    {
        modeName = BeatmapInfo.TrimCharacteristicString(modeName);
        BeatLeaderLeaderboard leaderboard = response.leaderboards.FirstOrDefault(x => x.difficulty.modeName == modeName && x.difficulty.difficultyName == difficultyName);
        if(leaderboard == null)
        {
            Debug.LogWarning($"Found no difficulty matching {modeName}, {difficultyName} in BeatLeader leaderboards!");
            return "";
        }
        return leaderboard.id;
    }


    private static BeatLeaderScore ParseScore(string json)
    {
        if(string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<BeatLeaderScore>(json);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to parse BeatLeader response with error: {err.Message}, {err.StackTrace}");
            ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Failed to parse API response!");
            return null;
        }
    }
}


[Serializable]
public class BeatLeaderScore
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

    public BeatLeaderScoreSongData song;
}


[Serializable]
public class BeatLeaderScoreSongData
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
public class BeatLeaderUser
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

    public BeatLeaderUserProfileSettings profileSettings;
}


[Serializable]
public class BeatLeaderUserProfileSettings
{
    public int id;
    public string leftSaberColor;
    public string rightSaberColor;
}


[Serializable]
public class BeatLeaderLeaderboardResponse
{
    public BeatLeaderSong song;
    public BeatLeaderLeaderboard[] leaderboards;
}


[Serializable]
public class BeatLeaderSong
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
public class BeatLeaderLeaderboard
{
    public string id;
    public BeatLeaderLeaderboardDifficulty difficulty;
}


[Serializable]
public class BeatLeaderLeaderboardDifficulty
{
    public int id;
    public int value;
    public int mode;
    public string difficultyName;
    public string modeName;
    public string status;
}
