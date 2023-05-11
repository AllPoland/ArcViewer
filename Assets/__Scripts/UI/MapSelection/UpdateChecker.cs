using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
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

    [SerializeField] private GameObject UpdateButton;

    private string versionString;


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
            return JsonConvert.DeserializeObject<Version>(json);
        }
        catch(Exception e)
        {
            Debug.LogWarning($"Failed to read json from {versionPath} with error: {e.Message}, {e.StackTrace}");
            return null;
        }
    }


    private static void SetLatestKnownVersion(Version knownVersion)
    {
        knownVersion.CheckedTime = DateTime.Now;
        string versionPath = LatestVersionPath;
        try
        {
            string json = JsonConvert.SerializeObject(knownVersion);
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
        if(!string.IsNullOrEmpty(versionString))
        {
            dialogueMessage = $"ArcViewer v{versionString} has released!\nWould you like to download it now?";
        }
        else
        {
            dialogueMessage = "A new version of ArcViewer has released!\nWould you like to download it now?";
        }
        DialogueHandler.ShowDialogueBox(DialogueBoxType.YesNo, dialogueMessage, OpenGithubPage);
    }


    public void UpdateVersion(Version currentVersion, Version knownVersion, Version latestVersion, bool updateKnownVersion = true)
    {
        if(latestVersion?.Name == null)
        {
            //Didn't get the latest version for some reason, so assume we know what the latest is
            latestVersion = knownVersion;
        }
        versionString = latestVersion.Name;

        if(knownVersion.IsEarlierThan(latestVersion))
        {
            //A new update has released since we last checked
            Debug.Log($"Found new version! {latestVersion.Name}");
            if(currentVersion.IsEarlierThan(latestVersion))
            {
                //Display a popup about the new update if we're out of date
                ShowUpdateDialogue();
            }
        }

        if(currentVersion.IsEarlierThan(latestVersion))
        {
            //There's a newer version than the current one,
            //enable the update button that'll annoy the user smil
            Debug.Log($"App version {Application.version} is outdated! Latest is {knownVersion.Name}");
            UpdateButton.SetActive(true);
        }
        else Debug.Log($"App version {Application.version} is up to date!");

        if(updateKnownVersion)
        {
            SetLatestKnownVersion(latestVersion);
        }
    }


    private IEnumerator CheckLatestVersionCoroutine()
    {
        UpdateButton.SetActive(false);
        Debug.Log("Checking for updates.");

        Version currentVersion = new Version {Name = Application.version};
        Version knownVersion = GetLatestKnownVersion();
        if(knownVersion?.Name == null)
        {
            Debug.Log("Found no known latest version, using current app version.");
            knownVersion = currentVersion;
            //Set this to a minimum datetime to ensure that the update is checked
            knownVersion.CheckedTime = DateTime.MinValue;
        }

        TimeSpan timeSinceCheck = DateTime.Now - knownVersion.CheckedTime;
        if(timeSinceCheck.TotalHours < 1f)
        {
            //Rate limit GitHub requests to one per hour
            Debug.Log($"Version has recently been checked. Using stored version: {knownVersion.Name}");
            UpdateVersion(currentVersion, knownVersion, null, false);
            yield break;
        }

        Debug.Log($"Checking GitHub for latest release from {LatestReleaseURL}");
        using UnityWebRequest uwr = UnityWebRequest.Get(LatestReleaseURL);
        yield return uwr.SendWebRequest();

        if(uwr.result != UnityWebRequest.Result.Success)
        {
            Debug.LogWarning($"Failed to get GitHub API response with error: {uwr.error}");
            UpdateVersion(currentVersion, knownVersion, null);
            yield break;
        }
        
        string json = uwr.downloadHandler.text;
        if(string.IsNullOrEmpty(json))
        {
            Debug.LogWarning($"Failed to get text from GitHub api response!");
            UpdateVersion(currentVersion, knownVersion, null);
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
            UpdateVersion(currentVersion, knownVersion, null);
            yield break;
        }

        if(string.IsNullOrEmpty(release.tag_name))
        {
            Debug.LogWarning($"Latest release has missing or empty tag name!");
            UpdateVersion(currentVersion, knownVersion, null);
            yield break;
        }

        //Trim out the extra stuff like "v" prefix and "-beta" suffix
        string versionText = release.tag_name.Split('-')[0].TrimStart('v');
        Debug.Log($"Found latest version: {versionText}");

        Version latestVersion = new Version {Name = versionText};
        UpdateVersion(currentVersion, knownVersion, latestVersion);
    }


    private void Start()
    {
#if !UNITY_WEBGL && !UNITY_EDITOR
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
    public DateTime CheckedTime;


    public bool IsEarlierThan(Version other)
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