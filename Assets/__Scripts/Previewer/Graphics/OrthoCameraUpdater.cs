using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrthoCameraUpdater : MonoBehaviour
{
    [SerializeField] private float cameraHeight = 1.4f;

    private Camera targetCamera;


    private void UpdateCameraPosition()
    {
        int cameraSide = SettingsManager.GetInt("orthocameraside");
        if(cameraSide == 0) 
        {
            //Back
            transform.rotation = Quaternion.identity;
            transform.position = new Vector3(0f, cameraHeight, -5f);
        }
        else if(cameraSide == 1)
        {
            //Right
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            transform.position = new Vector3(5f, cameraHeight, 0f);
        }
        else
        {
            //Left
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            transform.position = new Vector3(-5f, cameraHeight, 0f);
        }
    }


    private void UpdateSettings(string setting)
    {
        if(setting == "all" || setting == "useorthocamera" || setting == "orthocameraside")
        {
            targetCamera.enabled = SettingsManager.GetBool("useorthocamera");
            if(targetCamera.enabled)
            {
                UpdateCameraPosition();
            }
        }
    }


    private void UpdateUIState(UIState newState)
    {
        if(newState == UIState.Previewer && ReplayManager.IsReplayMode)
        {
            Enable();
        }
        else Disable();
    }

    
    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
    }


    private void Enable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        if(SettingsManager.Loaded)
        {
            UpdateSettings("all");
        }
    }


    private void Disable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
        targetCamera.enabled = false;
    }
}