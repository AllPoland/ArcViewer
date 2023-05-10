using UnityEngine;

[ExecuteInEditMode]
public class LightHandler : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private MeshRenderer glowRenderer;

    [Header("Parameters")]
    [SerializeField] private LightEventType type;
    [SerializeField] private int id;


    private void UpdateProperties(LightingPropertyEventArgs eventArgs)
    {
        if(eventArgs.type == type)
        {
            meshRenderer.SetPropertyBlock(eventArgs.laserProperties);
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


    private void Update()
    {
        UpdateRotation();
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