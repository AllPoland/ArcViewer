using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Bloomfog : ScriptableRendererFeature
{
    //These are static fields for graphics settings to access
    public static bool Enabled = true;
    public static int Quality = 2;

    [System.Serializable]
    public class BloomfogSettings
    {
        public Material prepassMaterial;

        [Space]
        public float bloomCaptureExtraFov = 0f;
        public float threshold = 1f;
        public float brightnessMult = 1f;
        public float attenuation = 1f;
        public float fogOffset = 0f;
        public float fogHeight = 0f;
        public float fogStartY = 0f;

        [Header("Blur Settings")]
        public Material blurMaterial;

        [Header("Output Settings")]
        public string outputTextureName;

        [Space]
        public BloomfogQualityPreset[] qualityPresets;

        [System.NonSerialized] public int textureWidth;
        [System.NonSerialized] public int textureHeight;
        [System.NonSerialized] public int actualDownsamplePasses;

        public BloomfogQualityPreset currentQualityPreset => qualityPresets[Mathf.Clamp(Quality, 0, qualityPresets.Length - 1)];
    }

    [System.Serializable]
    public class BloomfogQualityPreset
    {
        public int referenceScreenHeight = 1024;
        [Min(2)] public int downsamplePasses = 5;
        [Min(0f)] public float upsampleBlend = 20f;
        [Min(0)] public int ignoreUpsampleIndex = 1;
    }

    [SerializeField] private BloomfogSettings settings = new BloomfogSettings();

    private BloomFogPass bloomFogPass;


    public override void Create()
    {
        bloomFogPass = new BloomFogPass(settings);
        bloomFogPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

        Shader.SetGlobalFloat("_CustomFogAttenuation", settings.attenuation);
        Shader.SetGlobalFloat("_CustomFogOffset", settings.fogOffset);
        Shader.SetGlobalFloat("_CustomFogHeightFogHeight", settings.fogHeight);
        Shader.SetGlobalFloat("_CustomFogHeightFogStartY", settings.fogStartY);
    }


    public override void OnCameraPreCull(ScriptableRenderer renderer, in CameraData cameraData)
    {
        Camera mainCamera = Camera.main;
        Camera renderCamera = cameraData.camera;

        BloomfogQualityPreset qualitySettings = settings.currentQualityPreset;

        //Update the camera field of view
        renderCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView + settings.bloomCaptureExtraFov, 30, 160);
        renderCamera.allowMSAA = false;

        float verticalFov = Mathf.Deg2Rad * renderCamera.fieldOfView;
        float horizontalFov = 2 * Mathf.Atan(Mathf.Tan(verticalFov / 2) * renderCamera.aspect);

        //Calculate the new texture ratio based on camera fov
        float originalVertFov = Mathf.Deg2Rad * mainCamera.fieldOfView;
        float screenPlaneDistance = qualitySettings.referenceScreenHeight / 2 / Mathf.Tan(originalVertFov / 2);

        //Set the new texture size
        float textureWidth = Mathf.Tan(horizontalFov / 2) * screenPlaneDistance * 2;
        float textureHeight = Mathf.Tan(verticalFov / 2) * screenPlaneDistance * 2;

        float referenceWidth = qualitySettings.referenceScreenHeight * mainCamera.aspect;
        float widthRatio = referenceWidth / textureWidth;
        float heightRatio = (float)qualitySettings.referenceScreenHeight / textureHeight;

        // Debug.Log($"fov: {verticalFov} horizontal: {horizontalFov} width: {settings.textureWidth} height: {settings.textureHeight} ratio: {widthRatio}, {heightRatio}");

        settings.textureHeight = qualitySettings.referenceScreenHeight;
        settings.textureWidth = (int)referenceWidth;

        Shader.SetGlobalVector("_FogTextureToScreenRatio", new Vector2(widthRatio, heightRatio));
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(settings.blurMaterial && settings.prepassMaterial)
        {
            renderer.EnqueuePass(bloomFogPass);
        }
    }


    private class BloomFogPass : ScriptableRenderPass
    {
        private BloomfogSettings settings;

        private int[] tempIDs;
        private RenderTargetIdentifier[] tempRTs;


        public BloomFogPass(BloomfogSettings fogSettings)
        {
            settings = fogSettings;
        }


        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            BloomfogQualityPreset qualitySettings = settings.currentQualityPreset;

            int width = settings.textureWidth;
            int height = settings.textureHeight;

            //Clamp the blur passes so we don't downsample below a 2x2 texture
            int minDimension = Mathf.Min(width, height);
            int maxDownsample = Mathf.FloorToInt(Mathf.Log(minDimension, 2));
            settings.actualDownsamplePasses = Mathf.Clamp(qualitySettings.downsamplePasses, 2, maxDownsample);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if(!Enabled)
            {
                if(!string.IsNullOrEmpty(settings.outputTextureName))
                {
                    //Bloomfog shouldn't be used, just output a black texture
                    Shader.SetGlobalTexture(settings.outputTextureName, Texture2D.blackTexture);
                }
                return;
            }

            BloomfogQualityPreset qualitySettings = settings.currentQualityPreset;
            CommandBuffer cmd = CommandBufferPool.Get("BloomfogBlur");

            //Create our temporary render textures for blurring
            tempIDs = new int[settings.actualDownsamplePasses];
            tempRTs = new RenderTargetIdentifier[settings.actualDownsamplePasses];
            for(int i = 0; i < settings.actualDownsamplePasses; i++)
            {
                int downsample = (int)Mathf.Pow(2, i + 1);

                tempIDs[i] = Shader.PropertyToID("tempBlurRT" + i);
                cmd.GetTemporaryRT(tempIDs[i], settings.textureWidth / downsample, settings.textureHeight / downsample, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
                tempRTs[i] = new RenderTargetIdentifier(tempIDs[i]);

                //Clear the texture content in case it's been carried over from the last frame
                cmd.SetRenderTarget(tempRTs[i]);
                cmd.ClearRenderTarget(true, true, Color.black);
            }

            //Copy the source into the first temp texture, applying brightness threshold
            cmd.SetGlobalFloat("_Threshold", settings.threshold);
            cmd.SetGlobalFloat("_BrightnessMult", settings.brightnessMult);

            cmd.Blit(renderingData.cameraData.targetTexture, tempRTs[0], settings.prepassMaterial);

            //Blit the source image into smaller and smaller textures, applying some blur
            cmd.SetGlobalFloat("_Offset", 0.5f);
            cmd.SetGlobalFloat("_BlurAlpha", 1f);
            for(int i = 1; i < settings.actualDownsamplePasses; i++)
            {
                cmd.Blit(tempRTs[i - 1], tempRTs[i], settings.blurMaterial);
            }

            //Blit back up the chain, bringing the blurred image to the half res RT
            cmd.SetGlobalFloat("_Offset", 1f);
            for(int i = settings.actualDownsamplePasses - 1; i > 0; i--)
            {
                //Blend the low res texture with alpha, to create a custom falloff of brightness
                //Don't blend high res images to avoid reintroducing unblurred details
                float alpha = i <= qualitySettings.ignoreUpsampleIndex ? 1f : Mathf.Pow(0.5f, i / qualitySettings.upsampleBlend);
                cmd.SetGlobalFloat("_BlurAlpha", alpha);

                cmd.Blit(tempRTs[i], tempRTs[i - 1], settings.blurMaterial);
            }

            if(!string.IsNullOrEmpty(settings.outputTextureName))
            {
                cmd.SetGlobalTexture(settings.outputTextureName, tempRTs[0]);
            }

            //Release our temporary render textures
            //Don't release texture 0 because it's our output texture
            for(int i = 1; i < settings.actualDownsamplePasses; i++)
            {
                cmd.ReleaseTemporaryRT(tempIDs[i]);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }
}