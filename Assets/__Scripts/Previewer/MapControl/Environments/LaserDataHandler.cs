using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteInEditMode]
public class LaserDataHandler : MonoBehaviour
{
    [SerializeField] private LightHandler[] lasers;
    [SerializeField] private OcclusionMarker[] occlusionMarkers;
    
    private LaserLight[] laserLights;
    private ComputeBuffer laserBuffer;
    private int laserCount;

    private LaserLight[] occlusions;
    private ComputeBuffer occlusionBuffer;
    private int occlusionCount;


    private void ClearLaserBuffer()
    {
        laserBuffer?.Dispose();
        laserCount = 0;
        Shader.SetGlobalInt("_NumLaserLights", 0);
    }


    private void ClearOcclusionBuffer()
    {
        occlusionBuffer?.Dispose();
        occlusionCount = 0;
        Shader.SetGlobalInt("_NumOcclusionMarkers", 0);
    }


    private void UpdateLaserBuffer()
    {
#if UNITY_EDITOR
        float brightnessMult = Application.isPlaying ? SettingsManager.GetFloat("lightglowbrightness") : 1f;
#else
        float brightnessMult = SettingsManager.GetFloat("lightglowbrightness");
#endif

        if(brightnessMult < Mathf.Epsilon || lasers == null || lasers.Length == 0)
        {
            ClearLaserBuffer();
            return;
        }

        if(lasers.Length != laserCount)
        {
            //Update the buffer to contain the new amount of lasers
            ClearLaserBuffer();
            laserBuffer = new ComputeBuffer(lasers.Length, Marshal.SizeOf(typeof(LaserLight)));
            laserCount = lasers.Length;
            Shader.SetGlobalInt("_NumLaserLights", laserCount);

            //Populate the new array of lasers
            laserLights = new LaserLight[laserCount];
        }

        for(int i = 0; i < laserCount; i++)
        {
            LightHandler lightHandler = lasers[i];
            if(lightHandler == null || !lightHandler.enabled || !lightHandler.gameObject.activeInHierarchy)
            {
                laserLights[i].color = Color.clear;
                continue;
            }

            Transform lightTransform = lightHandler.transform;

            laserLights[i].color = lightHandler.CurrentColor * lightHandler.diffuseMult * brightnessMult;
            laserLights[i].origin = lightTransform.position;
            laserLights[i].direction = lightTransform.up;
            laserLights[i].halfLength = Mathf.Abs(lightTransform.lossyScale.y) / 2f;
            laserLights[i].brightnessCap = lightHandler.diffuseBrightnessCap;
            laserLights[i].falloff = lightHandler.diffuseFalloff;
            laserLights[i].falloffSteepness = lightHandler.diffuseFalloffSteepness;
        }

        //Send the new laser data to the GPU
        laserBuffer.SetData(laserLights);
        Shader.SetGlobalBuffer("_LaserLights", laserBuffer);
    }


    private void UpdateOcclusionBuffer()
    {
        if(occlusionMarkers == null || occlusionMarkers.Length == 0)
        {
            ClearOcclusionBuffer();
            return;
        }
    
        if(occlusionMarkers.Length != occlusionCount)
        {
            ClearOcclusionBuffer();
            occlusionBuffer = new ComputeBuffer(occlusionMarkers.Length, Marshal.SizeOf(typeof(LaserLight)));
            occlusionCount = occlusionMarkers.Length;
            Shader.SetGlobalInt("_NumOcclusionMarkers", occlusionCount);

            occlusions = new LaserLight[occlusionCount];
        }

        for(int i = 0; i < occlusionCount; i++)
        {
            OcclusionMarker occlusionMarker = occlusionMarkers[i];
            if(occlusionMarker == null || !occlusionMarker.enabled || !occlusionMarker.gameObject.activeInHierarchy)
            {
                occlusions[i].color = Color.clear;
                continue;
            }

            Transform occlusionTransform = occlusionMarker.transform;

            occlusions[i].color = Color.white * occlusionMarker.intensity;
            occlusions[i].origin = occlusionTransform.position;
            occlusions[i].direction = occlusionTransform.up;
            occlusions[i].halfLength = Mathf.Abs(occlusionTransform.lossyScale.y) / 2f;
            occlusions[i].brightnessCap = occlusionMarker.brightnessCap;
            occlusions[i].falloff = occlusionMarker.falloff;
            occlusions[i].falloffSteepness = occlusionMarker.falloffSteepness;
        }

        occlusionBuffer.SetData(occlusions);
        Shader.SetGlobalBuffer("_OcclusionMarkers", occlusionBuffer);
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
        UpdateBuffers();
    }


    private void OnDisable()
    {
        ClearLaserBuffer();
        ClearOcclusionBuffer();
    }
}


public struct LaserLight
{
    public Vector4 color;
    public Vector3 origin;
    public Vector3 direction;
    public float halfLength;
    public float brightnessCap;
    public float falloff;
    public float falloffSteepness;
}