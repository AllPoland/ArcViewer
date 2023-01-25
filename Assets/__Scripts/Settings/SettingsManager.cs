using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static Settings CurrentSettings { get; private set; }

    public static Action OnSettingsUpdated;

    private const string settingsFile = "UserSettings.json";

    private bool saving;


    private async Task WriteFileAsync(string text, string path)
    {
        await File.WriteAllTextAsync(path, text);
    }


    private IEnumerator SaveSettingsCoroutine()
    {
        saving = true;

        string filePath = Path.Combine(Application.persistentDataPath, settingsFile);
        //Need to use newtonsoft otherwise dictionaries don't serialize
        string json = JsonConvert.SerializeObject(CurrentSettings);

        Task writeTask = WriteFileAsync(json, filePath);
        yield return new WaitUntil(() => writeTask.IsCompleted);

        if(writeTask.Exception != null)
        {
            Debug.Log($"Failed to save settings with error: {writeTask.Exception.Message}, {writeTask.Exception.StackTrace}");
            ErrorHandler.Instance?.DisplayPopup(ErrorType.Error, "Failed to save your settings!");
        }

        saving = false;
    }


    public void SaveSettings()
    {
        if(saving)
        {
            Debug.Log("Trying to save settings when already saving!");
            return;
        }

        StartCoroutine(SaveSettingsCoroutine());
    }


    private void LoadSettings()
    {
        string filePath = Path.Combine(Application.persistentDataPath, settingsFile);
        
        if(!File.Exists(filePath))
        {
            Debug.Log("Settings file doesn't exist. Using defaults.");
            CurrentSettings = Settings.DefaultSettings;
            SaveSettings();

            OnSettingsUpdated?.Invoke();
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            CurrentSettings = JsonConvert.DeserializeObject<Settings>(json);
        }
        catch(Exception err)
        {
            ErrorHandler.Instance?.DisplayPopup(ErrorType.Error, "Failed to load settings! Reverting to default.");
            Debug.LogWarning($"Failed to load settings with error: {err.Message}, {err.StackTrace}");

            CurrentSettings = Settings.DefaultSettings;
            SaveSettings();
        }

        OnSettingsUpdated?.Invoke();
    }


    public static bool GetRuleBool(string name)
    {
        if(CurrentSettings.Bools.ContainsKey(name))
        {
            return CurrentSettings.Bools[name];
        }
        else return false;
    }


    public static int GetRuleInt(string name)
    {
        if(CurrentSettings.Ints.ContainsKey(name))
        {
            return CurrentSettings.Ints[name];
        }
        else return 0;
    }


    public static float GetRuleFloat(string name)
    {
        if(CurrentSettings.Floats.ContainsKey(name))
        {
            return CurrentSettings.Floats[name];
        }
        else return 0;
    }


    public static void AddRule(string name, bool value)
    {
        Dictionary<string, bool> rules = CurrentSettings.Bools;
        if(rules.ContainsKey(name))
        {
            rules[name] = value;
        }
        else
        {
            rules.Add(name, value);
        }
        OnSettingsUpdated?.Invoke();
    }


    public static void AddRule(string name, int value)
    {
        Dictionary<string, int> rules = CurrentSettings.Ints;
        if(rules.ContainsKey(name))
        {
            rules[name] = value;
        }
        else
        {
            rules.Add(name, value);
        }
        OnSettingsUpdated?.Invoke();
    }


    public static void AddRule(string name, float value)
    {
        Dictionary<string, float> rules = CurrentSettings.Floats;
        if(rules.ContainsKey(name))
        {
            rules[name] = value;
        }
        else
        {
            rules.Add(name, value);
        }
        OnSettingsUpdated?.Invoke();
    }


    private void Awake()
    {
        LoadSettings();
    }
}


[Serializable]
public class Settings
{
    public Dictionary<string, bool> Bools;
    public Dictionary<string, int> Ints;
    public Dictionary<string, float> Floats;


    public static Settings DefaultSettings = new Settings
    {
        Bools = new Dictionary<string, bool>
        {
            {"vsync", true}
        },

        Ints = new Dictionary<string, int>
        {
            {"framecap", 60}
        },

        Floats = new Dictionary<string, float>
        {

        }
    };
}