using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

//Base class for leaderboard services that replays can be loaded from
//Adding a new service means subclassing this and registering an instance in ReplaySources.All
public abstract class ReplaySource
{
    public abstract ReplaySourceType SourceType { get; }
    public abstract string Name { get; }

    //Score ID prefixes accepted in the map input field (i.e. "bl:12345")
    public abstract string[] InputPrefixes { get; }

    //Website root for this source, used for building profile/leaderboard links
    public abstract string BaseURL { get; }

    //API root used for this source's score lookups
    public abstract string ApiURL { get; }

    //URLs from this source that should bypass the CORS proxy
    public virtual string[] CorsURLs => new[] { BaseURL, ApiURL };

    //Whether the given website host belongs to this source
    public virtual bool MatchesHost(string host) => false;

    //Creates the base source info attached to replays from this source
    public abstract ReplaySourceInfo CreateInfo();

    //Resolves a score ID into replay/map download info through the source API
    public abstract Task<ResolvedScore> ResolveScoreAsync(string scoreID, string mapURL, string mapID, bool showErrors = true);

    //Tries to convert a link from this source's website into equivalent ArcViewer share arguments
    //Returns true when the link belongs to this source; convertedQuery is null when the link is invalid
    public virtual bool TryConvertLink(string url, out string convertedQuery)
    {
        convertedQuery = null;
        return false;
    }


    protected static string CombineArgument(string name, string value)
    {
        return string.Join('=', name, value);
    }


    protected static string GetQueryValue(List<KeyValuePair<string, string>> parameters, params string[] names)
    {
        foreach(string name in names)
        {
            KeyValuePair<string, string> match = parameters.FirstOrDefault(x => x.Key == name);
            if(!string.IsNullOrEmpty(match.Value))
            {
                return match.Value;
            }
        }

        return null;
    }
}


public static class ReplaySources
{
    public static readonly BeatLeaderSource BeatLeader = new BeatLeaderSource();
    public static readonly ScoreSaberSource ScoreSaber = new ScoreSaberSource();

    //Automatic score ID resolution is attempted in this order
    public static readonly ReplaySource[] All = { BeatLeader, ScoreSaber };


    public static ReplaySource FromType(ReplaySourceType sourceType)
    {
        return All.FirstOrDefault(x => x.SourceType == sourceType);
    }


    public static ReplaySource FromHost(string host)
    {
        return All.FirstOrDefault(x => x.MatchesHost(host));
    }


    public static bool TryParsePrefixedScoreID(string input, out ReplaySource source, out string scoreID)
    {
        input = input.Trim();

        foreach(ReplaySource candidate in All)
        {
            foreach(string prefix in candidate.InputPrefixes)
            {
                if(!input.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                string id = input[prefix.Length..].Trim();
                if(!string.IsNullOrEmpty(id) && id.All(char.IsDigit))
                {
                    source = candidate;
                    scoreID = id;
                    return true;
                }
            }
        }

        source = null;
        scoreID = null;
        return false;
    }
}
