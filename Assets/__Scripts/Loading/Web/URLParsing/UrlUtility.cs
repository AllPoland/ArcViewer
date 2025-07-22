using System.Collections.Generic;
using System.Linq;
using System.Web;

public static class UrlUtility
{
    //Array of all parameters specific to ArcViewer, which are most likely not
    //part of a sub-url
    public static readonly string[] useKeys =
    {
        "url",
        "id",
        "t",
        "mode",
        "difficulty",
        "noProxy",
        "replayURL",
        "scoreID",
        "scoreId",
        "link",
        "mapLink",
        "time",
        "firstPerson",
        "uiOff",
        "autoPlay",
        "loop"
    };


    public static List<KeyValuePair<string, string>> CombineUrlParams(List<KeyValuePair<string, string>> parameters)
    {
        List<KeyValuePair<string, string>> combined = new List<KeyValuePair<string, string>>();

        for(int i = 0; i < parameters.Count; i++)
        {
            KeyValuePair<string, string> pair = parameters[i];
            string key = pair.Key;
            string value = pair.Value;

            bool hasSubParams = false;
            while(i < parameters.Count - 1 && !useKeys.Contains(parameters[i + 1].Key))
            {
                //The next parameter isn't one that arcviewer uses,
                //so it's part of the current value

                //Combine this parameter with the current value, then skip over it
                i++;
                KeyValuePair<string, string> subPair = parameters[i];

                if(!string.IsNullOrEmpty(subPair.Key))
                {
                    char prefix = hasSubParams ? '&' : '?';
                    value += $"{prefix}{subPair.Key}={subPair.Value}";
                    hasSubParams = true;
                }
            }

            combined.Add(new KeyValuePair<string, string>(key, value));
        }

        return combined;
    }


    public static List<KeyValuePair<string, string>> ParseUrlParams(string url)
    {
        List<KeyValuePair<string, string>> parameters = new List<KeyValuePair<string, string>>();

        url = HttpUtility.UrlDecode(url);

        //Split the url by only its first question mark to get the params string
        string[] paramSplit = url.Split('?');
        paramSplit[0] = "";
        string paramString = string.Join('&', paramSplit).TrimStart('&');

        //Split the parameters into their separate components
        string[] paramStrings = paramString.Split('&');
        foreach(string p in paramStrings)
        {
            if(p.Count(c => c == '=') != 1)
            {
                //This parameter doesn't contain exactly one '=', so it's invalid
                continue;
            }

            //This is guaranteed to have a length of 2 since there's exactly one '='
            string[] components = p.Split('=');
            parameters.Add(new KeyValuePair<string, string>(components[0], components[1]));
        }

        return CombineUrlParams(parameters);
    }
}