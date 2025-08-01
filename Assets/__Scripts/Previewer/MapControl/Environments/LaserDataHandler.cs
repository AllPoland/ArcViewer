using UnityEngine;

[ExecuteInEditMode]
public class LaserDataHandler : MonoBehaviour
{
    public const int MaxLasers = 15;
    public const int MaxOcclusions = 3;

    [SerializeField] private LightHandler[] lasers;
    [SerializeField] private OcclusionMarker[] occlusionMarkers;
    
    private Vector4[] laserColors;
    private Vector4[] laserOrigins;
    private Vector4[] laserDirections;
    private Vector4[] laserFalloffInfos;

    private Vector4[] occlusionColors;
    private Vector4[] occlusionOrigins;
    private Vector4[] occlusionDirections;
    private Vector4[] occlusionFalloffInfos;


    private void UpdateLaserBuffer(bool forceUpdate = false)
    {
        float brightnessMult = SettingsManager.Loaded ? SettingsManager.GetFloat("lightglowbrightness") : 1f;

        if(laserColors == null || laserColors.Length != MaxLasers)
        {
            //Populate the new array of lasers
            laserColors = new Vector4[MaxLasers];
            laserOrigins = new Vector4[MaxLasers];
            laserDirections = new Vector4[MaxLasers];
            laserFalloffInfos = new Vector4[MaxLasers];

            if(lasers.Length > MaxLasers)
            {
                Debug.LogWarning($"{lasers.Length} light emitters is too many! Only {MaxLasers} are allowed.");
            }
        }

        bool dirty = false;
        for(int i = 0; i < MaxLasers; i++)
        {
            if(i >= lasers.Length)
            {
                if(laserColors[i] != Vector4.zero)
                {
                    laserColors[i] = Vector4.zero;
                    dirty = true;
                }
                continue;
            }

            LightHandler lightHandler = lasers[i];
            if(lightHandler == null || !lightHandler.enabled || !lightHandler.gameObject.activeInHierarchy)
            {
                if(laserColors[i] != Vector4.zero)
                {
                    laserColors[i] = Vector4.zero;
                    dirty = true;
                }
                continue;
            }

            if(!lightHandler.Dirty && !forceUpdate)
            {
                continue;
            }

            Transform lightTransform = lightHandler.transform;

            laserColors[i] = lightHandler.CurrentColor * lightHandler.diffuseMult * brightnessMult;
            laserOrigins[i] = lightTransform.position;
            laserDirections[i] = lightTransform.up;

            Vector4 falloffInfo = new Vector4
            {
                x = Mathf.Abs(lightTransform.lossyScale.y) / 2f,
                y = lightHandler.diffuseBrightnessCap,
                z = lightHandler.diffuseFalloff,
                w = lightHandler.diffuseFalloffSteepness
            };
            laserFalloffInfos[i] = falloffInfo;

            lightHandler.Dirty = false;
            dirty = true;
        }

        if(dirty)
        {
            //Send the new laser data to the GPU
            Shader.SetGlobalVectorArray("_LaserColors", laserColors);
            Shader.SetGlobalVectorArray("_LaserOrigins", laserOrigins);
            Shader.SetGlobalVectorArray("_LaserDirections", laserDirections);
            Shader.SetGlobalVectorArray("_LaserFalloffInfos", laserFalloffInfos);
        }
    }


    private void UpdateOcclusionBuffer(bool forceUpdate = false)
    {
        if(occlusionColors == null || occlusionColors.Length != MaxOcclusions)
        {
            occlusionColors = new Vector4[MaxOcclusions];
            occlusionOrigins = new Vector4[MaxOcclusions];
            occlusionDirections = new Vector4[MaxOcclusions];
            occlusionFalloffInfos = new Vector4[MaxOcclusions];

            if(occlusionMarkers.Length > MaxOcclusions)
            {
                Debug.LogWarning($"{occlusionMarkers.Length} occlusion markers is too many! Only {MaxOcclusions} are allowed.");
            }
        }

        bool dirty = false;
        for(int i = 0; i < MaxOcclusions; i++)
        {
            if(i >= occlusionMarkers.Length)
            {
                if(occlusionColors[i] != Vector4.zero)
                {
                    occlusionColors[i] = Vector4.zero;
                    dirty = true;
                }
                continue;
            }

            OcclusionMarker occlusionMarker = occlusionMarkers[i];
            if(occlusionMarker == null || !occlusionMarker.enabled || !occlusionMarker.gameObject.activeInHierarchy)
            {
                if(occlusionColors[i] != Vector4.zero)
                {
                    occlusionColors[i] = Vector4.zero;
                    dirty = true;
                }
                continue;
            }

            if(!occlusionMarker.Dirty && !forceUpdate)
            {
                continue;
            }

            Transform occlusionTransform = occlusionMarker.transform;

            occlusionColors[i] = Color.white * occlusionMarker.intensity;
            occlusionOrigins[i] = occlusionTransform.position;
            occlusionDirections[i] = occlusionTransform.up;

            Vector4 falloffInfo = new Vector4
            {
                x = Mathf.Abs(occlusionTransform.lossyScale.y) / 2f,
                y = occlusionMarker.brightnessCap,
                z = occlusionMarker.falloff,
                w = occlusionMarker.falloffSteepness
            };
            occlusionFalloffInfos[i] = falloffInfo;

            occlusionMarker.Dirty = false;
            dirty = true;
        }

        if(dirty)
        {
            Shader.SetGlobalVectorArray("_OcclusionColors", occlusionColors);
            Shader.SetGlobalVectorArray("_OcclusionOrigins", occlusionOrigins);
            Shader.SetGlobalVectorArray("_OcclusionDirections", occlusionDirections);
            Shader.SetGlobalVectorArray("_OcclusionFalloffInfos", occlusionFalloffInfos);
        }
    }


    private void UpdateSettings(string setting)
    {
        if(setting == "all" || setting == "lightglowbrightness")
        {
            //Force update the laser buffer to account for change in brightness settings
            UpdateLaserBuffer(true);
        }
    }


    private void UpdateBuffers()
    {
        UpdateLaserBuffer();
        UpdateOcclusionBuffer();
    }


    private void LateUpdate()
    {
        UpdateBuffers();
    }


    private void OnEnable()
    {
        SettingsManager.OnSettingsUpdated += UpdateSettings;
        UpdateBuffers();
    }


    private void OnDisable()
    {
        SettingsManager.OnSettingsUpdated -= UpdateSettings;
    }
}