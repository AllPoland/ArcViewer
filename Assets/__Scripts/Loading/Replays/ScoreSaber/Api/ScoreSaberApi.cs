using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

#pragma warning disable CS4014
#pragma warning disable 1998
public static class ScoreSaberApi
{
    public static string ReplayURLFromID(string scoreID)
    {
        return $"{ReplaySources.ScoreSaber.ApiURL}scores/{Uri.EscapeDataString(scoreID)}/replay";
    }

    public static async Task<ScoreSaberScoreResponse> ScoreFromID(string scoreID, bool showErrors = true)
    {
        string url = $"{ReplaySources.ScoreSaber.ApiURL}scores/{Uri.EscapeDataString(scoreID)}?includeScoreStats=false";
#if UNITY_WEBGL && !UNITY_EDITOR
        url = WebLoader.GetCorsURL(url);
#endif

        try
        {
            using UnityWebRequest uwr = UnityWebRequest.Get(url);
            uwr.SendWebRequest();

            while(!uwr.isDone) await Task.Yield();

            if(uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"Failed to get ScoreSaber API response with error: {uwr.error}");
                if(showErrors)
                {
                    ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find ScoreSaber score {scoreID}! {uwr.error}");
                }
                return null;
            }

            return ParseResponse(uwr.downloadHandler.text, showErrors);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to get ScoreSaber API response with error: {err.Message}, {err.StackTrace}");
            if(showErrors)
            {
                ErrorHandler.Instance.QueuePopup(ErrorType.Error, $"Couldn't find ScoreSaber score {scoreID}! {err.Message}");
            }
            return null;
        }
    }


    private static ScoreSaberScoreResponse ParseResponse(string json, bool showErrors)
    {
        if(string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("ScoreSaber score API response is empty!");
            return null;
        }

        try
        {
            return JsonConvert.DeserializeObject<ScoreSaberScoreResponse>(json);
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to parse ScoreSaber response with error: {err.Message}, {err.StackTrace}");
            if(showErrors)
            {
                ErrorHandler.Instance.QueuePopup(ErrorType.Error, "Failed to parse ScoreSaber API response!");
            }
            return null;
        }
    }
}

[Serializable]
public class ScoreSaberScoreResponse
{
    public ScoreSaberScoreInfo score;
    public ScoreSaberLeaderboardInfo leaderboard;
    public object scoreStats;

    public ScoreSaberPlayerInfo GetPlayer()
    {
        return score?.player;
    }
}

[Serializable]
public class ScoreSaberPlayerInfo
{
    public string id;
    public string name;
    public string avatar;
    public string country;
}

[Serializable]
public class ScoreSaberScoreInfo
{
    public int id;
    public int baseScore;
    public int unmodifiedScore;
    public int modifiedScore;
    public float accuracy;
    public float pp;
    public float weight;
    public string modifiers;
    public string[] mods;
    public bool fullCombo;
    public int missedNotes;
    public int badCuts;
    public int maxCombo;
    public bool hasReplay;
    public bool personalBest;
    public string createdAt;
    public ScoreSaberPlayerInfo player;
}

[Serializable]
public class ScoreSaberLeaderboardInfo
{
    public int id;
    public ScoreSaberMapInfo map;
    public ScoreSaberDifficultyInfo difficulty;
}

[Serializable]
public class ScoreSaberMapInfo
{
    public int id;
    public string hash;
    public string bsid;
    public string songName;
    public string songSubName;
    public string songAuthorName;
    public string levelAuthorName;
}

[Serializable]
public class ScoreSaberDifficultyInfo
{
    public int id;
    public int difficulty;
    public string rawDifficulty;
    public string gameMode;
}
