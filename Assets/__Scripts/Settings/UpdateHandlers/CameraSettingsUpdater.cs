using System.Collections.Generic;
using UnityEngine;

public class CameraSettingsUpdater : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private List<Camera> affectedCameras;


    public void UpdateCameraSettings()
    {
        float cameraZ = SettingsManager.GetFloat("cameraposition");
        int cameraTilt = SettingsManager.GetInt("cameratilt");
        int fov = SettingsManager.GetInt("camerafov");

        cameraTransform.localPosition = new Vector3(cameraTransform.localPosition.x, cameraTransform.localPosition.y, cameraZ);
        //Camera tilt is flipped because positive x tilts down for some reason
        cameraTransform.eulerAngles = new Vector3(-cameraTilt, cameraTransform.eulerAngles.y, cameraTransform.eulerAngles.z);

        foreach(Camera camera in affectedCameras)
        {
            camera.fieldOfView = fov;
        }
    }


    private void Start()
    {
        SettingsManager.OnSettingsUpdated += UpdateCameraSettings;

        UpdateCameraSettings();
    }
}