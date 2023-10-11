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
        public Material outputMaterial;
        public string outputTextureName;

        [Space]
        public BloomfogQualityPreset[] qualityPresets;

        [System.NonSerialized] public int textureWidth;
        [System.NonSerialized] public int textureHeight;
        [System.NonSerialized] public int actualDownsamplePasses;

        public BloomfogQualityPreset currentQualityPreset => qualityPresets[Mathf.Clamp(Bloomfog.Quality, 0, qualityPresets.Length - 1)];
        public int referenceScreenHeight => currentQualityPreset.referenceScreenHeight;
        public int downsamplePasses => currentQualityPreset.downsamplePasses;
        public float upsampleBlend => currentQualityPreset.upsampleBlend;
        public int ignoreUpsampleIndex => currentQualityPreset.ignoreUpsampleIndex;
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

    private CameraConfigPass cameraConfigPass;
    private BloomFogPass bloomFogPass;


    public override void Create()
    {
        cameraConfigPass = new CameraConfigPass(settings);
        cameraConfigPass.renderPassEvent = RenderPassEvent.BeforeRendering;

        bloomFogPass = new BloomFogPass(settings);
        bloomFogPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        Shader.SetGlobalFloat("_CustomFogAttenuation", settings.attenuation);
        Shader.SetGlobalFloat("_CustomFogOffset", settings.fogOffset);
        Shader.SetGlobalFloat("_CustomFogHeightFogHeight", settings.fogHeight);
        Shader.SetGlobalFloat("_CustomFogHeightFogStartY", settings.fogStartY);
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(settings.blurMaterial && settings.outputMaterial && settings.prepassMaterial)
        {
            renderer.EnqueuePass(cameraConfigPass);

            bloomFogPass.SourceTexture = renderer.cameraColorTarget;
            renderer.EnqueuePass(bloomFogPass);
        }
    }


    private class CameraConfigPass : ScriptableRenderPass
    {
        private BloomfogSettings settings;

        
        public CameraConfigPass(BloomfogSettings fogSettings)
        {
            settings = fogSettings;
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            Camera mainCamera = Camera.main;
            Camera renderCamera = renderingData.cameraData.camera;

            //Update the camera field of view
            renderCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView + settings.bloomCaptureExtraFov, 30, 160);

            float verticalFov = Mathf.Deg2Rad * renderCamera.fieldOfView;
            float horizontalFov = 2 * Mathf.Atan(Mathf.Tan(verticalFov / 2) * renderCamera.aspect);

            //Calculate the new texture ratio based on camera fov
            float originalVertFov = Mathf.Deg2Rad * mainCamera.fieldOfView;
            float screenPlaneDistance = (settings.referenceScreenHeight / 2) / Mathf.Tan(originalVertFov / 2);

            //Set the new texture size
            settings.textureWidth = Mathf.RoundToInt(Mathf.Tan(horizontalFov / 2) * screenPlaneDistance * 2);
            settings.textureHeight = Mathf.RoundToInt(Mathf.Tan(verticalFov / 2) * screenPlaneDistance * 2);

            float referenceWidth = settings.referenceScreenHeight * mainCamera.aspect;
            float widthRatio = referenceWidth / settings.textureWidth;
            float heightRatio = (float)settings.referenceScreenHeight / settings.textureHeight;

            // Debug.Log($"fov: {verticalFov} horizontal: {horizontalFov} width: {settings.textureWidth} height: {settings.textureHeight} ratio: {widthRatio}, {heightRatio}");

            Shader.SetGlobalVector("_FogTextureToScreenRatio", new Vector2(widthRatio, heightRatio));
        }
    }


    private class BloomFogPass : ScriptableRenderPass
    {
        public RenderTargetIdentifier SourceTexture;

        private BloomfogSettings settings;

        private int[] tempIDs;
        private RenderTargetIdentifier[] tempRTs;


        public BloomFogPass(BloomfogSettings fogSettings)
        {
            settings = fogSettings;
        }


        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int width = settings.textureWidth;
            int height = settings.textureHeight;

            int minDimension = Mathf.Min(width, height);
            int maxDownsample = Mathf.FloorToInt(Mathf.Log(minDimension, 2));
            settings.actualDownsamplePasses = Mathf.Clamp(settings.downsamplePasses, 2, maxDownsample);

            //Create our temporary render textures for blurring
            tempIDs = new int[settings.actualDownsamplePasses];
            tempRTs = new RenderTargetIdentifier[settings.actualDownsamplePasses];
            for(int i = 0; i < settings.actualDownsamplePasses; i++)
            {
                int downsample = (int)Mathf.Pow(2, i + 1);

                tempIDs[i] = Shader.PropertyToID("tempBlurRT" + i);
                cmd.GetTemporaryRT(tempIDs[i], width / downsample, height / downsample, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
                tempRTs[i] = new RenderTargetIdentifier(tempIDs[i]);
            }
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

            CommandBuffer cmd = CommandBufferPool.Get("BloomfogBlur");

            //Copy the source into the first temp texture, applying brightness threshold
            cmd.SetGlobalFloat("_Threshold", settings.threshold);
            cmd.SetGlobalFloat("_BrightnessMult", settings.brightnessMult);

            cmd.SetGlobalFloat("_Offset", 0.5f);
            cmd.SetGlobalFloat("_BlurAlpha", 1f);
            cmd.Blit(SourceTexture, tempRTs[0], settings.prepassMaterial);

            //Blit the source image into smaller and smaller textures, applying blur
            for(int i = 1; i < settings.actualDownsamplePasses; i++)
            {
                cmd.Blit(tempRTs[i - 1], tempRTs[i], settings.blurMaterial);
            }

            //Blit back up the chain, bringing the blurred image to the full res output
            cmd.SetGlobalFloat("_Offset", 1f);

            float upsampleBlend = settings.upsampleBlend;
            int ignoreUpsampleIndex = settings.ignoreUpsampleIndex;

            for(int i = settings.actualDownsamplePasses - 1; i > 0; i--)
            {
                //Blend the low res texture with alpha, to create a custom falloff of brightness
                //Don't blend high res images to avoid reintroducing unblurred details
                float alpha = i <= ignoreUpsampleIndex ? 1f : Mathf.Pow(0.5f, i / upsampleBlend);
                cmd.SetGlobalFloat("_BlurAlpha", alpha);

                cmd.Blit(tempRTs[i], tempRTs[i - 1], settings.blurMaterial);
            }

            //Clear the source texture so it doesn't have the original unblurred lights
            cmd.SetRenderTarget(SourceTexture);
            cmd.ClearRenderTarget(true, true, Color.black);

            //Final blit, outputting final blurred result
            cmd.Blit(tempRTs[0], SourceTexture, settings.outputMaterial);
            if(!string.IsNullOrEmpty(settings.outputTextureName))
            {
                cmd.SetGlobalTexture(settings.outputTextureName, SourceTexture);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }
}