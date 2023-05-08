using UnityEngine;

public class LightHandler : MonoBehaviour
{
    [SerializeField] private LightEventType type;
    [SerializeField] private int id;
    [SerializeField] private MeshRenderer meshRenderer;


    private void UpdateProperties(LightingPropertyEventArgs eventArgs)
    {
        if(eventArgs.type == type)
        {
            meshRenderer.SetPropertyBlock(eventArgs.properties);
        }
    }


    private void OnEnable()
    {
        LightManager.OnLightPropertiesChanged += UpdateProperties;
    }


    private void OnDisable()
    {
        LightManager.OnLightPropertiesChanged -= UpdateProperties;
    }
}