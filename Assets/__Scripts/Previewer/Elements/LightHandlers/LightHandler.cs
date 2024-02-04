using UnityEngine;

public class LightHandler : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer glowRenderer;

    [Header("Parameters")]
    [SerializeField] public LightEventType type;
    [SerializeField] public int id;
    [SerializeField] public float emissionMult = 1f;

    private MaterialPropertyBlock laserProperties;
    private MaterialPropertyBlock glowProperties;


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
            SetProperties(eventArgs.laserProperties, eventArgs.glowProperties);
        }
        else
        {
            //Either the current event or the next don't affect this ID
            //Find the last event that does - if any, and apply color based on that
            Color eventColor = Color.clear;

            LightEvent lightEvent = eventArgs.lightEvent;
            if(!useThisEvent)
            {
                lightEvent = lightEvent.GetLastEvent(id);
            }
            LightEvent nextEvent = useNextEvent ? eventArgs.nextEvent : lightEvent?.GetNextEvent(id);

            eventColor = LightManager.GetEventColor(lightEvent, nextEvent);

            eventArgs.sender.SetLightProperties(eventColor, ref laserProperties, ref glowProperties);
            UpdateProperties();
        }
    }


    private void SetProperties(MaterialPropertyBlock newLaserProperties, MaterialPropertyBlock newGlowProperties)
    {
        laserProperties.SetColor("_BaseColor", newLaserProperties.GetColor("_BaseColor"));
        glowProperties.SetColor("_BaseColor", newGlowProperties.GetColor("_BaseColor"));

        UpdateProperties();
    }


    private void UpdateProperties()
    {
        meshRenderer.SetPropertyBlock(laserProperties);
        glowRenderer.SetPropertyBlock(glowProperties);
    }


    private void Awake()
    {
        laserProperties = new MaterialPropertyBlock();
        glowProperties = new MaterialPropertyBlock();

        laserProperties.SetFloat("_ColorMult", emissionMult);
    }


    private void OnEnable()
    {
        LightManager.OnLightPropertiesChanged += UpdateLight;
    }


    private void OnDisable()
    {
        LightManager.OnLightPropertiesChanged -= UpdateLight;
    }
}