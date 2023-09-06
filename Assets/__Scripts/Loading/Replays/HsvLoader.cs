using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class HsvLoader : MonoBehaviour
{
    private static ScoreColorSettings _customHSV;
    public static ScoreColorSettings CustomHSV
    {
        get => _customHSV;
        private set
        {
            _customHSV = value;
            OnCustomHSVUpdated?.Invoke();
        }
    }

    public static string CustomConfigJson { get; private set; }
    public static bool ValidConfig { get; private set; }

    public static event Action OnCustomHSVUpdated;

    private const string HsvFile = "UserHSV.json";
    private string HsvPath => Path.Combine(Application.persistentDataPath, HsvFile);

    private bool savingHSV = false;
    private Coroutine saveCoroutine;


    private void SetHSVFromJson(string configJson)
    {
        CustomConfigJson = configJson;

        try
        {
            HsvConfig newConfig = JsonConvert.DeserializeObject<HsvConfig>(configJson);
            if(newConfig != null)
            {
                ValidConfig = true;
                CustomHSV = new ScoreColorSettings(newConfig);
            }
            else
            {
                Debug.LogWarning("Null HSV config!");
                ValidConfig = false;
                CustomHSV = new ScoreColorSettings();
            }
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to parse HSV config with error: {err.Message}");
            ValidConfig = false;
            CustomHSV = new ScoreColorSettings();
        }
    }


//Suppress warnings about a lack of await when building for WebGL
#pragma warning disable 1998
    private async Task SaveHSV(string configJson)
    {
        try
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            await File.WriteAllTextAsync(HsvPath, configJson);
#else
            PlayerPrefs.SetString("customhsv", configJson);
#endif
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to write HSV config with error: {err.Message}, {err.StackTrace}");
            ErrorHandler.Instance.ShowPopup(ErrorType.Error, "Failed to save HSV config to disk!");
        }
    }


    private async Task LoadHSV()
    {
        try
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            Debug.Log("Loading custom HSV from file.");

            string path = HsvPath;
            if(!File.Exists(path))
            {
                //The file doesn't exist, so just set defaults
                ValidConfig = false;
                CustomHSV = new ScoreColorSettings();
                CustomConfigJson = "";
                return;
            }

            string configJson = await File.ReadAllTextAsync(path, System.Text.Encoding.UTF8);
#else
            Debug.Log("Loading custom HSV from player prefs.");
            string configJson = PlayerPrefs.GetString("customhsv", "");
#endif

            if(string.IsNullOrEmpty(configJson))
            {
                ValidConfig = false;
                CustomHSV = new ScoreColorSettings();
                CustomConfigJson = "";
                return;
            }

            SetHSVFromJson(configJson);            
        }
        catch(Exception err)
        {
            Debug.LogWarning($"Failed to read HSV config with error: {err.Message}, {err.StackTrace}");
            ErrorHandler.Instance.ShowPopup(ErrorType.Warning, "Failed to read your HSV config!");

            ValidConfig = false;
            CustomHSV = new ScoreColorSettings();
            CustomConfigJson = "";
        }
    }


    private IEnumerator SetHSVCoroutine(string configJson)
    {
        savingHSV = true;

        using Task saveTask = SaveHSV(configJson);
        SetHSVFromJson(configJson);

        yield return new WaitUntil(() => saveTask.IsCompleted);

        savingHSV = false;
    }


    public void SetHSV(string configJson)
    {
        if(configJson == null)
        {
            throw new ArgumentNullException("configJson cannot be null!");
        }

        if(savingHSV)
        {
            StopCoroutine(saveCoroutine);
        }

        saveCoroutine = StartCoroutine(SetHSVCoroutine(configJson));
    }


    private async void Start()
    {
        await LoadHSV();
    }
}