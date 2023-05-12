using UnityEngine;

public class LightHandler : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer glowRenderer;

    [Header("Parameters")]
    [SerializeField] private LightEventType type;
    [SerializeField] private int id;
    [SerializeField] private Optional<Color> OffColor;

    private MaterialPropertyBlock properties;


    private void UpdateProperties(LightingPropertyEventArgs eventArgs)
    {
        if(eventArgs.type == type)
        {
            if(!OffColor.Enabled)
            {
                meshRenderer.SetPropertyBlock(eventArgs.laserProperties);
            }
            else
            {
                //Used for lights that persist as physical elements when off
                Color baseColor = eventArgs.laserProperties.GetColor("_BaseColor");
                float alpha = baseColor.a;
                properties.SetColor("_EmissionColor", baseColor.SetValue(eventArgs.emission * alpha, true));

                Color newColor = Color.Lerp(baseColor, OffColor.Value, 1 - alpha);
                newColor.a = 1f;
                properties.SetColor("_BaseColor", newColor);

                meshRenderer.SetPropertyBlock(properties);
            }

            bool enableGlow = eventArgs.glowProperties.GetFloat("_Alpha") > 0.001f;
            SetGlowActive(enableGlow);
            if(enableGlow)
            {
                glowRenderer.SetPropertyBlock(eventArgs.glowProperties);
            }
        }
    }


    private void SetGlowActive(bool active)
    {
        if(glowRenderer && glowRenderer.gameObject.activeInHierarchy != active)
        {
            glowRenderer.gameObject.SetActive(active);
        }
    }


    public void UpdateGlowRotation()
    {
        if(!glowRenderer)
        {
            return;
        }

        Vector3 lookDirection = glowRenderer.transform.position - Camera.main.transform.position;
        lookDirection = glowRenderer.transform.InverseTransformDirection(lookDirection);
        lookDirection.y = 0f;
        lookDirection = glowRenderer.transform.TransformDirection(lookDirection);

        glowRenderer.transform.rotation = Quaternion.LookRotation(lookDirection, glowRenderer.transform.up);

        //The x and z rotations can sometimes get nudged around and break things
        Vector3 eulerAngles = glowRenderer.transform.localEulerAngles;
        eulerAngles.x = 0f;
        eulerAngles.z = 0f;
        glowRenderer.transform.localEulerAngles = eulerAngles;
    }


    private void Awake()
    {
        if(OffColor.Enabled)
        {
            properties = new MaterialPropertyBlock();
        }
    }


    private void OnEnable()
    {
        LightManager.OnLightPropertiesChanged += UpdateProperties;
        CameraSettingsUpdater.OnCameraPositionUpdated += UpdateGlowRotation;

        UpdateGlowRotation();
    }


    private void OnDisable()
    {
        LightManager.OnLightPropertiesChanged -= UpdateProperties;
        CameraSettingsUpdater.OnCameraPositionUpdated -= UpdateGlowRotation;
    }
}