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


    private void UpdateLight(LightingPropertyEventArgs eventArgs)
    {
        if(eventArgs.type != type)
        {
            return;
        }

        if(eventArgs.lightEvent?.AffectsID(id) ?? true && (eventArgs.nextEvent?.AffectsID(id) ?? true))
        {
            //The current event and next events affect this id, so no need to recalculate anything
            //This will always be the case in vanilla lightshows without lightID
            UpdateProperties(eventArgs.laserProperties, eventArgs.glowProperties);
        }
        else
        {
            //Either the current event or the next don't affect this ID
            //Find the last event that does - if any, and apply color based on that
            int startIndex = Mathf.Clamp(eventArgs.eventIndex, 0, eventArgs.eventList.Count - 1);
            int lastIndex = eventArgs.eventList.FindLastIndex(startIndex, x => x.AffectsID(id));

            Color eventColor = Color.clear;
            if(lastIndex >= 0)
            {
                LightEvent lightEvent = eventArgs.eventList[lastIndex];

                //Find the next event that affects this id to check for transition events
                startIndex = Mathf.Min(lastIndex + 1, eventArgs.eventList.Count - 1);
                int nextIndex = eventArgs.eventList.FindIndex(startIndex, x => x.AffectsID(id));
    
                LightEvent nextEvent = nextIndex >= 0 ? eventArgs.eventList[nextIndex] : null;

                eventColor = LightManager.GetEventColor(lightEvent, nextEvent);
            }

            //The fact that this has to route to the LightManager instance is yucky
            //but I don't know what to do about it so haha ball
            eventArgs.sender.SetLightProperties(eventColor, ref laserProperties, ref glowProperties);
            UpdateProperties(laserProperties, glowProperties);
        }
    }


    private void UpdateProperties(MaterialPropertyBlock newLaserProperties, MaterialPropertyBlock newGlowProperties)
    {
        meshRenderer.SetPropertyBlock(newLaserProperties);

        bool enableGlow = newGlowProperties.GetFloat("_Alpha") > 0.001f;
        SetGlowActive(enableGlow);
        if(enableGlow)
        {
            glowRenderer.SetPropertyBlock(newGlowProperties);
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
        laserProperties = new MaterialPropertyBlock();
        glowProperties = new MaterialPropertyBlock();
    }


    private void OnEnable()
    {
        LightManager.OnLightPropertiesChanged += UpdateLight;
        CameraSettingsUpdater.OnCameraPositionUpdated += UpdateGlowRotation;

        UpdateGlowRotation();
    }


    private void OnDisable()
    {
        LightManager.OnLightPropertiesChanged -= UpdateLight;
        CameraSettingsUpdater.OnCameraPositionUpdated -= UpdateGlowRotation;
    }
}