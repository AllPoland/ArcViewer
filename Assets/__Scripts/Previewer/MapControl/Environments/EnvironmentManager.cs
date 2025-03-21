using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnvironmentManager : MonoBehaviour
{
    private static bool _loading;
    public static bool Loading
    {
        get => _loading;
        private set
        {
            _loading = value;
        }
    }

    private static int _currentSceneIndex = -1;
    public static int CurrentSceneIndex
    {
        get => _currentSceneIndex;
        private set
        {
            _currentSceneIndex = value;
        }
    }

    public static event Action<int> OnEnvironmentUpdated;

    public static readonly string[] V2Environments = new string[]
    {
        "DefaultEnvironment",
        "OriginsEnvironment",
        "TriangleEnvironment",
        "NiceEnvironment",
        "BigMirrorEnvironment",
        "DragonsEnvironment",
        "KDAEnvironment",
        "MonstercatEnvironment",
        "CrabRaveEnvironment",
        "PanicEnvironment",
        "RocketEnvironment",
        "GreenDayEnvironment",
        "GreenDayGrenadeEnvironment",
        "TimbalandEnvironment",
        "FitBeatEnvironment",
        "LinkinParkEnvironment",
        "BTSEnvironment",
        "KaleidoscopeEnvironment",
        "InterscopeEnvironment",
        "SkrillexEnvironment",
        "BillieEnvironment",
        "HalloweenEnvironment",
        "GagaEnvironment"
    };

    public static readonly string[] V3Environments = new string[]
    {
        "WeaveEnvironment",
        "PyroEnvironment",
        "EDMEnvironment",
        "TheSecondEnvironment",
        "LizzoEnvironment",
        "TheWeekndEnvironment",
        "RockMixtapeEnvironment",
        "Dragons2Environment",
        "Panic2Environment",
        "QueenEnvironment",
        "LinkinPark2Environment",
        "TheRollingStonesEnvironment",
        "LatticeEnvironment",
        "DaftPunkEnvironment",
        "HipHopEnvironment",
        "ColliderEnvironment",
        "BritneyEnvironment",
        "Monstercat2Environment",
        "MetallicaEnvironment"
    };

    private static readonly string[] supportedEnvironments = new string[]
    {
        "DefaultEnvironment",
        "OriginsEnvironment",
        "TriangleEnvironment",
        "NiceEnvironment",
        "BigMirrorEnvironment",
        "DragonsEnvironment",
        "KDAEnvironment",
        "MonstercatEnvironment",
        "PanicEnvironment",
        "TimbalandEnvironment",
        "FitBeatEnvironment",
        "KaleidoscopeEnvironment"
    };

    private static readonly Dictionary<string, string> duplicateEnvironments = new Dictionary<string, string>
    {
        {"CrabRaveEnvironment", "MonstercatEnvironment"}
    };

    public static EnvironmentLightParameters CurrentEnvironmentParameters = new EnvironmentLightParameters();
    public static EnvironmentLightParameters DefaultEnvironmentParameters = new EnvironmentLightParameters();

    [SerializeField] private EnvironmentLightParameters[] EnvironmentParameters;

    private const int defaultSceneIndex = 1;

    private int targetSceneIndex = -1;


    private IEnumerator LoadEnvironment()
    {
        Loading = true;
        
        while(CurrentSceneIndex != targetSceneIndex)
        {
            if(CurrentSceneIndex >= 0)
            {
                Debug.Log("Unloading old scene.");
                AsyncOperation unloadTask = SceneManager.UnloadSceneAsync(CurrentSceneIndex);

                while(!unloadTask.isDone)
                {
                    yield return null;
                }
            }

            int sceneIndex = targetSceneIndex;
            Debug.Log($"Loading new environment scene: {targetSceneIndex}");
            AsyncOperation loadTask = SceneManager.LoadSceneAsync(sceneIndex, LoadSceneMode.Additive);

            while(!loadTask.isDone)
            {
                yield return null;
            }

            CurrentSceneIndex = sceneIndex;
            if(CurrentSceneIndex != targetSceneIndex)
            {
                Debug.Log($"Target scene was changed from {CurrentSceneIndex} to {targetSceneIndex} while loading.");
            }
        }

        Debug.Log("Environment loaded.");
        Loading = false;

        OnEnvironmentUpdated?.Invoke(CurrentSceneIndex);
    }


    private void SetEnvironment(string environmentName)
    {
        if(SettingsManager.GetBool("environmentoverride"))
        {
            int customIndex = SettingsManager.GetInt("customenvironment");
            customIndex = Mathf.Clamp(customIndex, 0, supportedEnvironments.Length - 1);
            environmentName = supportedEnvironments[customIndex];
        }

        if(duplicateEnvironments.ContainsKey(environmentName))
        {
            //Some environments look identical, so the same scene gets loaded for them
            environmentName = duplicateEnvironments[environmentName];
        }

        Debug.Log($"Setting new environment: {environmentName}");
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(environmentName);
        if(sceneIndex < 0)
        {
            Debug.Log($"Found no scene match for {environmentName}, using default.");
            sceneIndex = defaultSceneIndex;
            environmentName = supportedEnvironments[0];
        }

        if(sceneIndex == targetSceneIndex && sceneIndex == CurrentSceneIndex)
        {
            Debug.Log($"Correct scene is already loaded.");
            return;
        }

        targetSceneIndex = sceneIndex;
        try
        {
            CurrentEnvironmentParameters = EnvironmentParameters.First(x => x.EnvironmentName == environmentName);
        }
        catch(InvalidOperationException)
        {
            //Missing parameters for this environment
            Debug.LogWarning($"Missing environment parameters for {environmentName}!");
            CurrentEnvironmentParameters = new EnvironmentLightParameters();
        }

        if(!Loading)
        {
            StartCoroutine(LoadEnvironment());
        }
    }


    private void UpdateDifficulty(Difficulty difficulty)
    {
        string environmentName = BeatmapManager.EnvironmentName;
        Debug.Log($"Map has environment: {environmentName}");

        if(duplicateEnvironments.ContainsKey(environmentName))
        {
            //Some environments look identical, so the same scene gets loaded for them
            environmentName = duplicateEnvironments[environmentName];
        }

        try
        {
            DefaultEnvironmentParameters = EnvironmentParameters.First(x => x.EnvironmentName == environmentName);
        }
        catch(InvalidOperationException)
        {
            //Missing parameters for this environment
            Debug.LogWarning($"Missing environment parameters for {environmentName}!");
            DefaultEnvironmentParameters = new EnvironmentLightParameters();
        }

        SetEnvironment(environmentName);
    }


    private void UpdateSettings(string setting)
    {
        if(setting == "all" || setting == "environmentoverride" || setting == "customenvironment")
        {
            SetEnvironment(BeatmapManager.EnvironmentName);
        }
    }


    private void Start()
    {
        BeatmapManager.OnBeatmapDifficultyChanged += UpdateDifficulty;
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        
        UpdateDifficulty(BeatmapManager.CurrentDifficulty);
    }
}


[Serializable]
public class EnvironmentLightParameters
{
    public string EnvironmentName = "DefaultEnvironment";

    [Space]
    public int RotatingLaserCount = 4;
    public bool RandomizeRotatingLasers = true;
    public float RotatingLaserStep = 0f;

    [Space]
    public float SmallRingRotationSpeed = 2f;
    public float SmallRingRotationAmount = 90f;
    public float SmallRingRotationProp = 1f;
    public float SmallRingMaxStep = 5f;
    public float SmallRingStartAngle = -45f;
    public float SmallRingStartStep = 3f;

    [Space]
    public float BigRingRotationSpeed = 2f;
    public float BigRingRotationAmount = 45f;
    public float BigRingRotationProp = 1f;
    public float BigRingMaxStep = 5f;
    public float BigRingStartAngle = -45f;
    public float BigRingStartStep = 0f;

    [Space]
    public float ZoomSpeed = 2f;
    public float CloseZoomStep = 2f;
    public float FarZoomStep = 5f;
    public bool StartRingZoomParity = true;

    [Space]
    public string[] SmallRingNameFilters = {"SmallTrackLaneRings"};
    public string[] BigRingNameFilters = {"BigTrackLaneRings"};
}