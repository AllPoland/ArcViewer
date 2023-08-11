using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class TrackShaderUpdater : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float defaultSmoothness;
    [SerializeField, Range(0f, 1f)] private float mirrorSmoothness;

    private MaterialPropertyBlock properties;
    private MeshRenderer meshRenderer;


    private void UpdateSettings(string setting)
    {
        if(setting == "all" || setting == "trackmirror" || setting == "dynamicreflections")
        {
            bool mirror = SettingsManager.GetBool("trackmirror") && SettingsManager.GetBool("dynamicreflections");
            float smoothness = mirror ? mirrorSmoothness : defaultSmoothness;

            properties.SetFloat("_Smoothness", smoothness);
            meshRenderer.SetPropertyBlock(properties);
        }
    }


    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        properties = new MaterialPropertyBlock();
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;

        if(SettingsManager.Loaded)
        {
            UpdateSettings("all");
        }
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
    }
}