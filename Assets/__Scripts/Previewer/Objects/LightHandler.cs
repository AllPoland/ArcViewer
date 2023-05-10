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
                baseColor.a = 1f;
                Color newColor = Color.Lerp(baseColor, OffColor.Value, 1 - alpha);

                properties.SetColor("_BaseColor", newColor);
                properties.SetColor("_EmissionColor", newColor.SetValue(eventArgs.emission * alpha, true));
                meshRenderer.SetPropertyBlock(properties);
            }
            glowRenderer.SetPropertyBlock(eventArgs.glowProperties);
        }
    }


    private void UpdateRotation()
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
        CameraSettingsUpdater.OnCameraPositionUpdated += UpdateRotation;

        UpdateRotation();
    }


    private void OnDisable()
    {
        LightManager.OnLightPropertiesChanged -= UpdateProperties;
        CameraSettingsUpdater.OnCameraPositionUpdated -= UpdateRotation;
    }
}