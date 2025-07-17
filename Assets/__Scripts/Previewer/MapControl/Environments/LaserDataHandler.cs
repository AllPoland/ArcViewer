using System.Runtime.InteropServices;
using UnityEngine;

[ExecuteInEditMode]
public class LaserDataHandler : MonoBehaviour
{
    [SerializeField] private LightHandler[] lasers;
    
    private LaserLight[] laserLights;
    private ComputeBuffer laserBuffer;
    private int laserCount;


    private void ClearLaserBuffer()
    {
        laserBuffer?.Dispose();
        laserCount = 0;
        Shader.SetGlobalInt("_NumLaserLights", 0);
    }


    private void UpdateLaserBuffer()
    {
        if(lasers == null || lasers.Length == 0)
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
            Shader.SetGlobalFloat("_NumLaserLights", laserCount);

            //Populate the new array of lasers
            laserLights = new LaserLight[laserCount];
        }

        for(int i = 0; i < laserCount; i++)
        {
            LightHandler lightHandler = lasers[i];
            if(lightHandler == null)
            {
                continue;
            }

            Transform lightTransform = lightHandler.transform;

            laserLights[i].color = lightHandler.CurrentColor * lightHandler.diffuseMult;
            laserLights[i].origin = lightTransform.position;
            laserLights[i].direction = lightTransform.up;
            laserLights[i].halfLength = Mathf.Abs(lightTransform.lossyScale.y) / 2f;
            laserLights[i].range = lightHandler.diffuseRange;
            laserLights[i].falloff = lightHandler.diffuseFalloff;
        }

        //Send the new laser data to the GPU
        laserBuffer.SetData(laserLights);
        Shader.SetGlobalBuffer("_LaserLights", laserBuffer);
    }


    private void LateUpdate()
    {
        UpdateLaserBuffer();
    }


    private void OnEnable()
    {
        UpdateLaserBuffer();
    }


    private void OnDisable()
    {
        ClearLaserBuffer();
    }
}


public struct LaserLight
{
    public Vector4 color;
    public Vector3 origin;
    public Vector3 direction;
    public float halfLength;
    public float range;
    public float falloff;
}