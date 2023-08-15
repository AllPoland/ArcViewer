using UnityEngine;

public class LightHandler : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer glowRenderer;

    [Header("Parameters")]
    [SerializeField] public LightEventType type;
    [SerializeField] public int id;

    private MaterialPropertyBlock laserProperties;
    private MaterialPropertyBlock glowProperties;

    private Vector3 glowBaseScale;


    private void UpdateLight(LightingPropertyEventArgs eventArgs)
    {
        if(eventArgs.type != type)
        {
            return;
        }

        bool useThisEvent = eventArgs.lightEvent?.AffectsID(id) ?? true;
        bool useNextEvent = eventArgs.nextEvent?.AffectsID(id) ?? true;
        if(useThisEvent && useNextEvent)
        {
            //The current event and next events affect this id, so no need to recalculate anything
            //This will always be the case in vanilla lightshows without lightID
            UpdateProperties(eventArgs.laserProperties, eventArgs.glowProperties, eventArgs.glowBrightness);
        }
        else
        {
            //Either the current event or the next don't affect this ID
            //Find the last event that does - if any, and apply color based on that
            Color eventColor = Color.clear;
            float glowBrightness = 0f;

            LightEvent lightEvent = eventArgs.lightEvent;
            if(!useThisEvent)
            {
                lightEvent = lightEvent.GetLastEvent(id);
            }

            LightEvent nextEvent = useNextEvent ? eventArgs.nextEvent : lightEvent?.GetNextEvent(id);
            eventColor = LightManager.GetEventColor(lightEvent, nextEvent);

            float v;
            Color.RGBToHSV(eventColor, out _, out _, out v);
            glowBrightness = v * eventColor.a;

            eventArgs.sender.SetLightProperties(eventColor, glowBrightness, ref laserProperties, ref glowProperties);
            UpdateProperties(laserProperties, glowProperties, glowBrightness);
        }
    }


    private void UpdateProperties(MaterialPropertyBlock newLaserProperties, MaterialPropertyBlock newGlowProperties, float glowBrightness)
    {
        meshRenderer.SetPropertyBlock(newLaserProperties);

        bool enableGlow = glowBrightness > 0.001f;
        SetGlowActive(enableGlow);
        if(enableGlow)
        {
            glowRenderer.SetPropertyBlock(newGlowProperties);
            UpdateGlowScale(glowBrightness);
        }
    }


    private void SetGlowActive(bool active)
    {
        if(glowRenderer && glowRenderer.gameObject.activeInHierarchy != active)
        {
            glowRenderer.gameObject.SetActive(active);
        }
    }


    private void UpdateGlowScale(float brightness)
    {
        if(brightness <= 1f)
        {
            glowRenderer.transform.localScale = glowBaseScale;
        }
        else
        {
            Vector2 baseScaleDifference = (Vector2)glowBaseScale - Vector2.one;
            baseScaleDifference.x = Mathf.Max(baseScaleDifference.x, 0f);
            baseScaleDifference.y = Mathf.Max(baseScaleDifference.y, 0f);

            Vector2 newScaleAmount = baseScaleDifference * brightness;
            glowRenderer.transform.localScale = Vector3.one + (Vector3)newScaleAmount;
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
        laserProperties = new MaterialPropertyBlock();
        glowProperties = new MaterialPropertyBlock();
    }


    private void OnEnable()
    {
        LightManager.OnLightPropertiesChanged += UpdateLight;
        CameraUpdater.OnCameraPositionUpdated += UpdateGlowRotation;

        glowBaseScale = glowRenderer.transform.localScale;

        UpdateGlowRotation();
    }


    private void OnDisable()
    {
        LightManager.OnLightPropertiesChanged -= UpdateLight;
        CameraUpdater.OnCameraPositionUpdated -= UpdateGlowRotation;
    }
}