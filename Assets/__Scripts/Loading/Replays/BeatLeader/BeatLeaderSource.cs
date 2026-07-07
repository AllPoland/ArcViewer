using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;

public class BeatLeaderSource : ReplaySource
{
    public override ReplaySourceType SourceType => ReplaySourceType.BeatLeader;
    public override string Name => "BeatLeader";
    public override string[] InputPrefixes => new[] { "bl:", "beatleader:" };
    public override string BaseURL => "https://beatleader.com/";
    public override string ApiURL => "https://api.beatleader.com/";


    public override ReplaySourceInfo CreateInfo()
    {
        ReplaySourceInfo info = new ReplaySourceInfo
        {
            SourceType = ReplaySourceType.BeatLeader
        };
        info.LoadSourceData = replay => LoadSourceDataAsync(info, replay);
        return info;
    }


    public override bool TryConvertLink(string url, out string convertedQuery)
    {
        convertedQuery = null;
        if(!url.StartsWith(UrlArgHandler.BeatLeaderViewerURL) && !url.StartsWith(UrlArgHandler.OldBeatLeaderViewerURL))
        {
            return false;
        }

        //Convert BeatLeader viewer arguments to their ArcViewer equivalents
        List<KeyValuePair<string, string>> parameters = UrlUtility.ParseUrlParams(HttpUtility.UrlDecode(url));
        List<string> convertedArgs = new List<string>();
        foreach(KeyValuePair<string, string> pair in parameters)
        {
            string value = pair.Value;
            switch(pair.Key)
            {
                case "scoreId":
                    convertedArgs.Add(CombineArgument("scoreID", value));
                    break;
                case "link":
                    convertedArgs.Add(CombineArgument("replayURL", value));
                    break;
                case "mapLink":
                    convertedArgs.Add(CombineArgument("url", value));
                    break;
                case "time":
                    if(int.TryParse(value, out int result))
                    {
                        //BL stores timestamps in ms, while ArcViewer uses seconds
                        value = ((float)result / 1000).ToString();
                        convertedArgs.Add(CombineArgument("t", value));
                    }
                    break;
            }
        }

        if(convertedArgs.Count > 0)
        {
            convertedQuery = string.Join('&', convertedArgs);
        }
        return true;
    }


    public override async Task<ResolvedScore> ResolveScoreAsync(string scoreID, string mapURL, string mapID, bool showErrors = true)
    {
        BeatLeaderScore apiResponse = await BeatLeaderApi.ScoreFromID(scoreID, showErrors);
        if(string.IsNullOrEmpty(apiResponse?.replay))
        {
            return null;
        }

        bool useMapID = !string.IsNullOrEmpty(mapID) && mapID != apiResponse.song?.id;
        bool useResponseURL = !useMapID && string.IsNullOrEmpty(mapURL) && !string.IsNullOrEmpty(apiResponse.song?.downloadUrl);

        if(useResponseURL && !BeatSaverHandler.BeatSaverCdnURLs.Any(x => apiResponse.song.downloadUrl.Contains(x)))
        {
            mapURL = System.Web.HttpUtility.UrlDecode(apiResponse.song.downloadUrl);
            UrlArgHandler.ignoreMapForSharing = true;

            if(mapID == apiResponse.song.id)
            {
                mapID = null;
            }
        }

        return new ResolvedScore
        {
            ReplayURL = System.Web.HttpUtility.UrlDecode(apiResponse.replay),
            MapURL = mapURL,
            MapID = mapID,
            SourceInfo = CreateInfo()
        };
    }


    public async Task LoadSourceDataAsync(ReplaySourceInfo source, Replay replay)
    {
        Debug.Log($"Getting BeatLeader user {replay.info.playerID}");
        BeatLeaderUser user = await BeatLeaderApi.UserFromID(replay.info.playerID);

        if(user != null)
        {
            ApplyUser(source, user);

            string avatarUrl = user.avatar;
#if UNITY_WEBGL && !UNITY_EDITOR
            avatarUrl = WebLoader.GetCorsURL(avatarUrl);
#endif
            byte[] avatarData = await ReplayLoader.DownloadAvatarData(avatarUrl);
            if(avatarData != null && avatarData.Length > 0)
            {
                ReplayManager.SetAvatarImageData(avatarData);
            }

            ReplayManager.ApplyCustomColorsFromSource();
        }

        string mapHash = replay.info.hash;
        if(!string.IsNullOrEmpty(mapHash) && mapHash.Length > 40)
        {
            mapHash = mapHash[..40];
        }

        if(!string.IsNullOrEmpty(mapHash))
        {
            Debug.Log("Getting replay leaderboard info.");
            BeatLeaderLeaderboardResponse leaderboard = await BeatLeaderApi.LeaderboardFromHash(mapHash);
            if(leaderboard != null)
            {
                string leaderboardID = BeatLeaderApi.LeaderboardIDFromResponse(leaderboard, replay.info.mode, replay.info.difficulty);
                ApplyLeaderboard(source, leaderboardID);

                if(!string.IsNullOrEmpty(leaderboard.song?.downloadUrl))
                {
                    source.FallbackMapDownloadURL = leaderboard.song.downloadUrl;
                    source.FallbackMapID = leaderboard.song.id;
                }
            }
        }
    }


    private static void ApplyUser(ReplaySourceInfo source, BeatLeaderUser user)
    {
        if(user == null) return;

        source.PlayerName = user.name;
        source.PlayerID = user.id;
        source.AvatarURL = user.avatar;

        if(!string.IsNullOrEmpty(user.id))
        {
            source.PlayerProfileURL = $"{ReplaySources.BeatLeader.BaseURL}u/{user.id}";
        }

        if(user.profileSettings != null
            && !string.IsNullOrEmpty(user.profileSettings.leftSaberColor)
            && !string.IsNullOrEmpty(user.profileSettings.rightSaberColor))
        {
            if(ColorUtility.TryParseHtmlString(user.profileSettings.leftSaberColor, out Color left)
                && ColorUtility.TryParseHtmlString(user.profileSettings.rightSaberColor, out Color right))
            {
                source.LeftSaberColor = left;
                source.RightSaberColor = right;
            }
        }
    }


    private static void ApplyLeaderboard(ReplaySourceInfo source, string leaderboardID)
    {
        if(!string.IsNullOrEmpty(leaderboardID))
        {
            source.LeaderboardURL = $"{ReplaySources.BeatLeader.BaseURL}leaderboard/global/{leaderboardID}";
        }
    }
}
