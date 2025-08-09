using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraUpdater : MonoBehaviour
{
    private static bool _freecam = false;
    public static bool Freecam
    {
        get => _freecam;
        set
        {
            if(value == _freecam)
            {
                return;
            }

            _freecam = value && UIStateManager.CurrentState == UIState.Previewer;
            OnFreecamUpdated?.Invoke();
        }
    }

    public static event Action OnFreecamUpdated;

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private List<Camera> affectedCameras;
    [SerializeField] private FreecamController freecamController;

    private string[] previewSettings = new string[]
    {
        "cameraposition",
        "cameratilt",
        "playerheight"
    };

    private string[] replaySettings = new string[]
    {
        "replaycameraposition",
        "replaycameratilt",
        "replaycameraheight",
        "firstpersonreplay"
    };

    private static bool firstPerson;

    private Vector3 fpCameraVelocity = Vector3.zero;
    private Quaternion fpCameraRotationDeriv = new Quaternion(0f, 0f, 0f, 0f);


    private void UpdateFov()
    {
        int fov;
        if(Freecam)
        {
            fov = SettingsManager.GetInt("freecamfov");
        }
        else if(ReplayManager.IsReplayMode)
        {
            fov = SettingsManager.GetBool("firstpersonreplay") ? SettingsManager.GetInt("fpcamerafov") : SettingsManager.GetInt("replaycamerafov");
        }
        else fov = SettingsManager.GetInt("camerafov");

        foreach(Camera camera in affectedCameras)
        {
            camera.fieldOfView = Mathf.Clamp(fov, 40f, 100f);
        }
    }


    private void SetPreviewCamera()
    {
        firstPerson = false;

        float cameraZ = Mathf.Clamp(SettingsManager.GetFloat("cameraposition"), -4f, 0f);
        int cameraTilt = Mathf.Clamp(SettingsManager.GetInt("cameratilt"), -30, 30);

        //Players' eyes aren't at the top of their head, so the camera is placed
        //10cm below the given player height
        const float eyeOffset = -0.1f;
        float cameraY = Mathf.Clamp(SettingsManager.GetFloat("playerheight"), 1.2f, 2.4f) + eyeOffset;

        cameraTransform.localPosition = new Vector3(0f, cameraY, cameraZ);
        cameraTransform.eulerAngles = new Vector3(-cameraTilt, 0f, 0f);
    }


    private void SetReplayCamera()
    {
        firstPerson = false;

        float cameraY = Mathf.Clamp(SettingsManager.GetFloat("replaycameraheight"), 0.5f, 3f);
        float cameraZ = Mathf.Clamp(SettingsManager.GetFloat("replaycameraposition"), -5f, 0f);
        int cameraTilt = Mathf.Clamp(SettingsManager.GetInt("replaycameratilt"), -30, 30);

        cameraTransform.localPosition = new Vector3(0f, cameraY, cameraZ);
        cameraTransform.eulerAngles = new Vector3(-cameraTilt, 0f, 0f);
    }


    private void SetFirstPersonCamera()
    {
        Freecam = false;
        firstPerson = true;

        fpCameraVelocity = Vector3.zero;
        fpCameraRotationDeriv = new Quaternion(0f, 0f, 0f, 0f);

        UpdateFirstPersonCamera(false);
    }


    private void UpdateFirstPersonCamera(bool smoothing)
    {
        Vector3 headPosition = PlayerPositionManager.HeadPosition;
        Quaternion headRotation = PlayerPositionManager.HeadRotation;

        float cameraOffset = Mathf.Clamp(SettingsManager.GetFloat("fpcameraposition"), 0f, 3f);
        headPosition.z -= cameraOffset;

        Vector3 eulerAngles = headRotation.eulerAngles;
        if(SettingsManager.GetBool("forcefpcameraupright"))
        {
            eulerAngles.x = 0f;
        }
        int xRotOffset = Mathf.Clamp(SettingsManager.GetInt("fpcamerarotoffset"), -30, 30);
        eulerAngles.x -= xRotOffset;

        headRotation = Quaternion.Euler(eulerAngles);

        Vector3 cameraPosition = cameraTransform.position;
        Quaternion cameraRotation = cameraTransform.rotation;
        if(smoothing)
        {
            //Smooth out camera motions
            //This is a rare case where I actually don't want this to be deterministic
            //Cause then scrubbing would suck
            float smoothAmount = Mathf.Clamp01(SettingsManager.GetFloat("fpcameramovementsmoothing"));
            float rotSmoothAmount = Mathf.Clamp01(SettingsManager.GetFloat("fpcamerarotationsmoothing"));

            cameraPosition = Vector3.SmoothDamp(cameraPosition, headPosition, ref fpCameraVelocity, smoothAmount);
            cameraRotation = cameraRotation.SmoothDamp(headRotation, ref fpCameraRotationDeriv, rotSmoothAmount);
        }
        else
        {
            cameraPosition = headPosition;
            cameraRotation = headRotation;
        }

        cameraTransform.position = cameraPosition;
        cameraTransform.rotation = cameraRotation;
    }


    private void UpdateFreecam()
    {
        freecamController.enabled = Freecam;

        if(!Freecam)
        {
            if(ReplayManager.IsReplayMode)
            {
                SetReplayCamera();
            }
            else SetPreviewCamera();
        }
        else if(firstPerson)
        {
            //Enabling freecam disables first person
            firstPerson = false;

            //Hack to avoid resetting the camera while still notifying other subscribers
            SettingsManager.OnSettingsUpdated -= UpdateCameraSettings;
            SettingsManager.SetRule("firstpersonreplay", false);
            SettingsManager.OnSettingsUpdated += UpdateCameraSettings;
        }

        UpdateFov();
    }


    public void UpdateCameraSettings(string setting)
    {
        bool allSettings = setting == "all";

        if(ReplayManager.IsReplayMode)
        {
            if(SettingsManager.GetBool("firstpersonreplay"))
            {
                if(allSettings || setting == "firstpersonreplay")
                {
                    SetFirstPersonCamera();
                }
            }
            else if(!Freecam && (allSettings || replaySettings.Contains(setting)))
            {
                SetReplayCamera();
            }
        }
        else if(!Freecam && (allSettings || previewSettings.Contains(setting)))
        {
            SetPreviewCamera();
        }

        if(allSettings || setting == "firstpersonreplay" || setting == "camerafov" || setting == "replaycamerafov" || setting == "fpcamerafov" || setting == "freecamfov")
        {
            UpdateFov();
        }
    }


    private void UpdateUIState(UIState newState)
    {
        Freecam = false;
    }


    private void LateUpdate()
    {
        if(firstPerson)
        {
            UpdateFirstPersonCamera(true);
        }
    }


    private void Start()
    {
        freecamController.enabled = false;

        UIStateManager.OnUIStateChanged += UpdateUIState;
        SettingsManager.OnSettingsUpdated += UpdateCameraSettings;
        ReplayManager.OnReplayModeChanged += (_) => UpdateCameraSettings("all");
        OnFreecamUpdated += UpdateFreecam;

        UpdateCameraSettings("all");
    }
}