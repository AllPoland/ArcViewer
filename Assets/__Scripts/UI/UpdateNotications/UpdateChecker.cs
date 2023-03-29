using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class UpdateChecker : MonoBehaviour
{
    public const string GithubAPI = "https://api.github.com/";
    public const string RepoURL = "repos/AllPoland/ArcViewer/releases/latest";
    public static string LatestReleaseURL => GithubAPI + RepoURL;

    public const string latestReleasePage = "https://github.com/AllPoland/ArcViewer/releases/latest";

    public const string VersionFile = "version.json";
    public static string LatestVersionPath => Path.Combine(Application.persistentDataPath, VersionFile);

    public static Version LatestVersion;

    [SerializeField] private GameObject UpdateButton;


    private static Version GetLatestKnownVersion()
    {
        string versionPath = LatestVersionPath;
        if(!File.Exists(versionPath))
        {
            return null;
        }

        try
        {
            string json = File.ReadAllText(versionPath);
            return JsonUtility.FromJson<Version>(json);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to read json from {versionPath} with error: {e.Message}, {e.StackTrace}");
            return null;
        }
    }


    private static void SetLatestKnownVersion(Version knownVersion)
    {
        string versionPath = LatestVersionPath;
        try
        {
            string json = JsonUtility.ToJson(knownVersion);
            File.WriteAllText(versionPath, json);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to write json to {versionPath} with error: {e.Message}, {e.StackTrace}");
        }
    }


    public void OpenGithubPage(DialogueResponse response)
    {
        if(response == DialogueResponse.Yes)
        {
            Debug.Log("Opening release page and closing.");
            Application.OpenURL(latestReleasePage);
            Application.Quit();
        }
    }


    public void ShowUpdateDialogue()
    {
        string dialogueMessage;
        if(LatestVersion?.Name != null)
        {
            dialogueMessage = $"ArcViewer v{LatestVersion.Name} has released!\nWould you like to download it now?";
        }
        else
        {
            dialogueMessage = "A new version of ArcViewer has released!\nWould you like to download it now?";
        }
        DialogueHandler.ShowDialogueBox(DialogueBoxType.YesNo, dialogueMessage, OpenGithubPage);
    }


    public void UpdateVersion()
    {
        Version currentVersion = new Version {Name = Application.version};
        Version knownVersion = GetLatestKnownVersion();
        if(knownVersion?.Name == null)
        {
            Debug.Log("Found no known latest version, using current app version.");
            knownVersion = currentVersion;
        }

        if(LatestVersion?.Name == null)
        {
            //Failed to get the latest version for some reason, so just assume we know what the latest is
            LatestVersion = knownVersion;
        }
        else if(knownVersion.CheckLaterVersion(LatestVersion))
        {
            //A new update has released since we last checked
            //Display a popup about it, even if we're multiple versions behind
            Debug.Log($"Found new version! {LatestVersion.Name}");
            ShowUpdateDialogue();
            knownVersion = LatestVersion;
        }

        if(currentVersion.CheckLaterVersion(knownVersion))
        {
            //There's a newer version than the current version,
            //Enable the update button that'll annoy the user smil
            Debug.Log($"Current app version {Application.version} is outdated! Latest is {knownVersion.Name}");
            UpdateButton.SetActive(true);
        }
        else Debug.Log("App version is up to date!");

        SetLatestKnownVersion(knownVersion);
    }


    private IEnumerator CheckLatestVersionCoroutine()
    {
        UpdateButton.SetActive(false);
        LatestVersion = null;

        Debug.Log($"Checking GitHub for latest release from {LatestReleaseURL}");
        using UnityWebRequest uwr = UnityWebRequest.Get(LatestReleaseURL);
        yield return uwr.SendWebRequest();

        if(uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Failed to get GitHub API response with error: {uwr.error}");
            UpdateVersion();
            yield break;
        }
        
        string json = uwr.downloadHandler.text;
        if(string.IsNullOrEmpty(json))
        {
            Debug.LogWarning($"Failed to get text from GitHub api response!");
            UpdateVersion();
            yield break;
        }

        GithubRelease release;
        try
        {
            release = JsonUtility.FromJson<GithubRelease>(json);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to parse GitHub api Response with error: {e.Message}, {e.StackTrace}");
            UpdateVersion();
            yield break;
        }

        if(string.IsNullOrEmpty(release.tag_name))
        {
            Debug.LogWarning($"Latest release has missing or empty tag name!");
            UpdateVersion();
            yield break;
        }

        //Trim out the extra stuff like "v" prefix and "-beta" suffix
        string versionText = release.tag_name.Split('-')[0].TrimStart('v');
        LatestVersion = new Version {Name = versionText};
        Debug.Log($"Found latest version: {versionText}");

        UpdateVersion();
    }


    private void Start()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        StartCoroutine(CheckLatestVersionCoroutine());
#else
        UpdateButton.SetActive(false);
#endif
    }
}


[Serializable]
public class Version
{
    public string Name;


    public bool CheckLaterVersion(Version other)
    {
        //Returns true if other is a later version
        //Returns false if other is an earlier or equal version

        //Split the semantic version to its sub-versions for looping
        string[] selfPatches = Name.Split('.');
        string[] otherPatches = other.Name.Split('.');

        //Avoids index out of range if one version has more subversions
        int minPatchCount = Mathf.Min(selfPatches.Length, otherPatches.Length);
        for(int i = 0; i < minPatchCount; i++)
        {
            int selfPatch;
            int otherPatch;

            if(!int.TryParse(selfPatches[i], out selfPatch))
            {
                Debug.LogWarning($"Version text {Name} is not correctly formatted!");
                //Favor the other version if the current one isn't formatted correctly
                return true;
            }
            if(!int.TryParse(otherPatches[i], out otherPatch))
            {
                Debug.LogWarning($"Version text {other.Name} is not correctly formatted!");
                //Favor the current version and don't update to one that isn't formatted correctly
                return false;
            }

            //The comparison is done as soon as a mismatched number is found
            if(otherPatch > selfPatch)
            {
                return true;
            }
            if(selfPatch > otherPatch)
            {
                return false;
            }
        }

        //If the loop didn't find any inequalities, if one is longer it's considered later
        if(otherPatches.Length > selfPatches.Length)
        {
            return true;
        }
        if(selfPatches.Length > otherPatches.Length)
        {
            return false;
        }

        return false;
    }
}


[Serializable]
public struct GithubRelease
{
    public string url;
    public string assets_url;
    public string upload_url;
    public string html_url;
    public int id;
    //author omitted because I'm lazy
    public string node_id;
    public string tag_name;
    public string target_commitish;
    public string name;
    public bool draft;
    public bool prerelease;
    public DateTime created_at;
    public DateTime published_at;
    //assets omitted because I'm lazy^2
    public string tarball_url;
    public string zipball_url;
    public string body;
}