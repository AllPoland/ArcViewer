using System;
using System.Collections.Generic;
using UnityEngine;

public class CameraSettingsUpdater : MonoBehaviour
{
    public static event Action OnCameraPositionUpdated;

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private List<Camera> affectedCameras;


    public void UpdateCameraSettings(string setting)
    {
        bool allSettings = setting == "all";

        if(ReplayManager.IsReplayMode)
        {
            if(allSettings || setting == "replaycameraposition" || setting == "replaycameratilt" || setting == "replaycameraheight")
            {
                float cameraY = SettingsManager.GetFloat("replaycameraheight");
                float cameraZ = SettingsManager.GetFloat("replaycameraposition");
                int cameraTilt = SettingsManager.GetInt("replyacameratilt");

                cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraY, cameraZ);
                cameraTransform.eulerAngles = new Vector3(-cameraTilt, cameraTransform.eulerAngles.y, cameraTransform.eulerAngles.z);

                OnCameraPositionUpdated?.Invoke();
            }
        }
        else if(allSettings || setting == "cameraposition" || setting == "cameratilt" || setting == "playerheight")
        {
            float cameraZ = SettingsManager.GetFloat("cameraposition");
            int cameraTilt = SettingsManager.GetInt("cameratilt");

            //Players' eyes aren't at the top of their head, so the camera is placed
            //10cm below the given player height
            const float eyeOffset = -0.1f;
            float cameraY = SettingsManager.GetFloat("playerheight") + eyeOffset;

            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraY, cameraZ);
            cameraTransform.eulerAngles = new Vector3(-cameraTilt, cameraTransform.eulerAngles.y, cameraTransform.eulerAngles.z);

            OnCameraPositionUpdated?.Invoke();
        }

        if(allSettings || setting == "camerafov" || setting == "replaycamerafov")
        {
            int fov = ReplayManager.IsReplayMode ? SettingsManager.GetInt("replaycamerafov") : SettingsManager.GetInt("camerafov");
            foreach(Camera camera in affectedCameras)
            {
                camera.fieldOfView = fov;
            }
        }
    }


    private void Start()
    {
        SettingsManager.OnSettingsUpdated += UpdateCameraSettings;
        ReplayManager.OnReplayModeChanged += (_) => UpdateCameraSettings("all");

        UpdateCameraSettings("all");
    }
}