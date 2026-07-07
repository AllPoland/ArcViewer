using System;
using System.Threading.Tasks;
using UnityEngine;

public class ScoreSaberSource : ReplaySource
{
    public override ReplaySourceType SourceType => ReplaySourceType.ScoreSaber;
    public override string Name => "ScoreSaber";
    public override string[] InputPrefixes => new[] { "ss:", "scoresaber:" };
    public override string BaseURL => "https://scoresaber.com/";
    public override string ApiURL => "https://scoresaber.com/api/v2/";
    public override string[] CorsURLs => new[] { BaseURL, ApiURL, "https://watch.scoresaber.com", "https://cdn.scoresaber.com" };


    public override bool MatchesHost(string host)
    {
        return host.Equals("scoresaber.com", StringComparison.InvariantCultureIgnoreCase)
            || host.Equals("watch.scoresaber.com", StringComparison.InvariantCultureIgnoreCase);
    }


    public override ReplaySourceInfo CreateInfo()
    {
        return CreateInfo(null);
    }


    public ReplaySourceInfo CreateInfo(ScoreSaberScoreResponse response)
    {
        ScoreSaberPlayerInfo player = response?.GetPlayer();
        ScoreSaberLeaderboardInfo leaderboard = response?.leaderboard;
        ReplaySourceInfo info = new ReplaySourceInfo
        {
            SourceType = ReplaySourceType.ScoreSaber,
            MapHash = leaderboard?.map?.hash,
            DifficultyRaw = leaderboard?.difficulty?.difficulty ?? 0,
            Characteristic = CharacteristicFromGameMode(leaderboard?.difficulty?.gameMode),
            MapID = leaderboard?.map?.bsid,
            PlayerName = player?.name,
            PlayerID = player?.id,
            AvatarURL = player?.avatar
        };

        if(!string.IsNullOrEmpty(info.PlayerID))
        {
            info.PlayerProfileURL = $"{BaseURL}u/{info.PlayerID}";
        }

        if(leaderboard?.map?.id > 0 && leaderboard.id > 0)
        {
            info.LeaderboardURL = $"{BaseURL}map/{leaderboard.map.id}/difficulty/{leaderboard.id}";
        }

        info.LoadSourceData = replay => LoadSourceDataAsync(info, replay);
        return info;
    }


    public override async Task<ResolvedScore> ResolveScoreAsync(string scoreID, string mapURL, string mapID, bool showErrors = true)
    {
        ScoreSaberScoreResponse apiResponse = await ScoreSaberApi.ScoreFromID(scoreID, showErrors);
        if(apiResponse == null || apiResponse.score == null || !apiResponse.score.hasReplay)
        {
            return null;
        }

        ReplaySourceInfo sourceInfo = CreateInfo(apiResponse);

        if(string.IsNullOrEmpty(mapID))
        {
            mapID = apiResponse.leaderboard?.map?.bsid;
        }

        return new ResolvedScore
        {
            ReplayURL = ScoreSaberApi.ReplayURLFromID(apiResponse.score?.id > 0 ? apiResponse.score.id.ToString() : scoreID),
            MapURL = mapURL,
            MapID = mapID,
            SourceInfo = sourceInfo
        };
    }


    public async Task LoadSourceDataAsync(ReplaySourceInfo source, Replay replay)
    {
        if(source == null || replay == null)
        {
            return;
        }

        if(!string.IsNullOrEmpty(replay.info?.playerID))
        {
            source.PlayerID = replay.info.playerID;
            source.PlayerProfileURL = $"{BaseURL}u/{source.PlayerID}";
        }

        if(string.IsNullOrEmpty(source.AvatarURL)) return;

        string avatarUrl = source.AvatarURL;
#if UNITY_WEBGL && !UNITY_EDITOR
        avatarUrl = WebLoader.GetCorsURL(avatarUrl);
#endif
        byte[] avatarData = await ReplayLoader.DownloadAvatarData(avatarUrl);
        if(avatarData != null && avatarData.Length > 0)
        {
            ReplayManager.SetAvatarImageData(avatarData);
        }
    }


    private static string CharacteristicFromGameMode(string gameMode)
    {
        if(string.IsNullOrEmpty(gameMode)) return null;
        if(!gameMode.StartsWith("Solo")) return gameMode;

        string characteristic = gameMode.Substring(4);
        return string.IsNullOrEmpty(characteristic) ? "Standard" : characteristic;
    }
}
