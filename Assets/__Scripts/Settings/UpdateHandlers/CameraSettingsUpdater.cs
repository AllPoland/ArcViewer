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
        if(allSettings || setting == "cameraposition" || setting == "cameratilt")
        {
            float cameraZ = SettingsManager.GetFloat("cameraposition");
            int cameraTilt = SettingsManager.GetInt("cameratilt");

            cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraTransform.localPosition.y, cameraZ);
            //Camera tilt is flipped because positive x tilts down for some reason
            cameraTransform.eulerAngles = new Vector3(-cameraTilt, cameraTransform.eulerAngles.y, cameraTransform.eulerAngles.z);

            OnCameraPositionUpdated?.Invoke();
        }

        if(allSettings || setting == "camerafov")
        {
            foreach(Camera camera in affectedCameras)
            {
                camera.fieldOfView = SettingsManager.GetInt("camerafov");
            }
        }
    }


    private void Start()
    {
        SettingsManager.OnSettingsUpdated += UpdateCameraSettings;

        UpdateCameraSettings("all");
    }
}