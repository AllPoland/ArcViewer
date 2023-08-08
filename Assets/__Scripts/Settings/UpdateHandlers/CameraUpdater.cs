using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraUpdater : MonoBehaviour
{
    public static event Action OnCameraPositionUpdated;

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private List<Camera> affectedCameras;

    private string[] replaySettings = new string[]
    {
        "replaycameraposition",
        "replaycameratilt",
        "replaycameraheight",
        "firstpersonreplay"
    };

    private bool firstPerson;

    private void SetPreviewCamera()
    {
        firstPerson = false;

        float cameraZ = SettingsManager.GetFloat("cameraposition");
        int cameraTilt = SettingsManager.GetInt("cameratilt");

        //Players' eyes aren't at the top of their head, so the camera is placed
        //10cm below the given player height
        const float eyeOffset = -0.1f;
        float cameraY = SettingsManager.GetFloat("playerheight") + eyeOffset;

        cameraTransform.localPosition = new Vector3(0f, cameraY, cameraZ);
        cameraTransform.eulerAngles = new Vector3(-cameraTilt, 0f, 0f);

        OnCameraPositionUpdated?.Invoke();
    }


    private void SetReplayCamera()
    {
        firstPerson = false;

        float cameraY = SettingsManager.GetFloat("replaycameraheight");
        float cameraZ = SettingsManager.GetFloat("replaycameraposition");
        int cameraTilt = SettingsManager.GetInt("replaycameratilt");

        cameraTransform.localPosition = new Vector3(0f, cameraY, cameraZ);
        cameraTransform.eulerAngles = new Vector3(-cameraTilt, 0f, 0f);

        OnCameraPositionUpdated?.Invoke();
    }


    private void SetFirstPersonCamera()
    {
        firstPerson = true;
        UpdateFirstPersonCamera(false);
    }


    private void UpdateFirstPersonCamera(bool smoothing)
    {
        Vector3 headPosition = PlayerPositionManager.HeadPosition;
        Quaternion headRotation = PlayerPositionManager.HeadRotation;

        float cameraOffset = SettingsManager.GetFloat("fpcameraposition");
        headPosition.z -= cameraOffset;

        if(SettingsManager.GetBool("forcefpcameraupright"))
        {
            Vector3 eulerAngles = headRotation.eulerAngles;
            eulerAngles.x = 0f;
            headRotation = Quaternion.Euler(eulerAngles);
        }

        Vector3 cameraPosition = cameraTransform.position;
        Quaternion cameraRotation = cameraTransform.rotation;
        if(smoothing)
        {
            //Smooth out camera motions
            //This is a rare case where I actually don't want this to be deterministic
            //Cause then scrubbing would suck
            float smoothAmount = SettingsManager.GetFloat("fpcamerasmoothing");
            float moveAmount = Mathf.Max(smoothAmount, 0.1f) / Mathf.Max(Time.deltaTime, 0.001f);

            float moveMagnitude = (cameraPosition - headPosition).magnitude;
            cameraPosition = Vector3.MoveTowards(cameraPosition, headPosition, moveMagnitude / moveAmount);

            float angleDifference = Quaternion.Angle(cameraRotation, headRotation);
            cameraRotation = Quaternion.RotateTowards(cameraRotation, headRotation, angleDifference / moveAmount);
        }
        else
        {
            cameraPosition = headPosition;
            cameraRotation = headRotation;
        }

        cameraTransform.position = cameraPosition;
        cameraTransform.rotation = cameraRotation;

        OnCameraPositionUpdated?.Invoke();
    }


    private void UpdateBeat(float _) => UpdateFirstPersonCamera(true);


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
            else if(allSettings || replaySettings.Contains(setting))
            {
                SetReplayCamera();
            }
        }
        else if(allSettings || setting == "cameraposition" || setting == "cameratilt" || setting == "playerheight")
        {
            SetPreviewCamera();
        }

        if(allSettings || setting == "camerafov" || setting == "replaycamerafov" || setting == "fpcamerafov")
        {
            int fov;
            if(ReplayManager.IsReplayMode)
            {
                fov = SettingsManager.GetBool("firstpersonreplay") ? SettingsManager.GetInt("fpcamerafov") : SettingsManager.GetInt("replaycamerafov");
            }
            else fov = SettingsManager.GetInt("camerafov");

            foreach(Camera camera in affectedCameras)
            {
                camera.fieldOfView = fov;
            }
        }
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
        SettingsManager.OnSettingsUpdated += UpdateCameraSettings;
        ReplayManager.OnReplayModeChanged += (_) => UpdateCameraSettings("all");

        UpdateCameraSettings("all");
    }
}