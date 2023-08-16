using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrthoCameraUpdater : MonoBehaviour
{
    [SerializeField] private float cameraHeight = 1.4f;
    [SerializeField] private float cameraDistance = 20f;

    private Camera targetCamera;
    private int cameraSide;


    private void UpdateClipPlane()
    {
        if(cameraSide == 0)
        {
            targetCamera.farClipPlane = BeatmapManager.HalfJumpDistance - transform.position.z;
        }
        else targetCamera.farClipPlane = 50f;
    }


    private void UpdateCameraPosition()
    {
        cameraSide = SettingsManager.GetInt("orthocameraside");
        if(cameraSide == 0) 
        {
            //Back
            transform.rotation = Quaternion.identity;
            transform.position = new Vector3(0f, cameraHeight, -cameraDistance);
        }
        else if(cameraSide == 1)
        {
            //Right
            transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            transform.position = new Vector3(cameraDistance, cameraHeight, 0f);
        }
        else
        {
            //Left
            transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            transform.position = new Vector3(-cameraDistance, cameraHeight, 0f);
        }
        UpdateClipPlane();
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


    private void OnEnable()
    {
        if(!targetCamera)
        {
            targetCamera = GetComponent<Camera>();
        }

        SettingsManager.OnSettingsUpdated += UpdateSettings;
        BeatmapManager.OnJumpSettingsChanged += UpdateClipPlane;
        if(SettingsManager.Loaded)
        {
            UpdateSettings("all");
        }
        else UpdateClipPlane();
    }
}