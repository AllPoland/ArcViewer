using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class Bloomfog : ScriptableRendererFeature
{
    [System.Serializable]
    public class BloomFogSettings
    {
        public Material thresholdMaterial;

        [Space]
        public float threshold = 1f;
        public float brightnessMult = 1f;
        public float attenuation = 1f;
        public float fogOffset = 0f;

        [Header("Blur Settings")]
        public int referenceScreenHeight = 720;
        [Min(2)] public int blurPasses = 2;
        [Min(1)] public int downsample = 1;
        public Material blurMaterial;

        [Header("Output Settings")]
        public Material outputMaterial;
        public string outputTextureName;
    }

    [SerializeField] private BloomFogSettings settings = new BloomFogSettings();

    private BloomFogPass bloomFogPass;


    public override void Create()
    {
        bloomFogPass = new BloomFogPass(settings);
        bloomFogPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        Shader.SetGlobalFloat("_CustomFogAttenuation", settings.attenuation);
        Shader.SetGlobalFloat("_CustomFogOffset", settings.fogOffset);
    }


    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if(settings.blurMaterial && settings.outputMaterial && settings.thresholdMaterial)
        {
            bloomFogPass.SourceTexture = renderer.cameraColorTarget;
            renderer.EnqueuePass(bloomFogPass);
        }
    }


    private class BloomFogPass : ScriptableRenderPass
    {
        public RenderTargetIdentifier SourceTexture;

        private LayerMask environmentLayerMask;
        private Material environmentMaskMaterial;
        private Material thresholdMaterial;
        private float threshold;
        private float brightnessMult;

        private Material blurMaterial;
        private int referenceHeight;
        private int passes;
        private int downsample;

        private string outputTextureName;
        private Material outputMaterial;

        private int tempID1;
        private int tempID2;

        private int maskID;

        private RenderTargetIdentifier tempRT1;
        private RenderTargetIdentifier tempRT2;

        private RenderTargetIdentifier maskRT;


        public BloomFogPass(BloomFogSettings settings)
        {
            thresholdMaterial = settings.thresholdMaterial;
            threshold = settings.threshold;
            brightnessMult = settings.brightnessMult;

            blurMaterial = settings.blurMaterial;
            referenceHeight = settings.referenceScreenHeight;
            passes = settings.blurPasses;
            downsample = settings.downsample;
            outputTextureName = settings.outputTextureName;

            outputMaterial = settings.outputMaterial;
        }


        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            int width = cameraTextureDescriptor.width;
            int height = cameraTextureDescriptor.height;

            float aspect = width / height;
            height = referenceHeight / downsample;
            width = Mathf.RoundToInt(referenceHeight * aspect) / downsample;

            //Create our temporary render textures for blurring
            tempID1 = Shader.PropertyToID("tempBlurRT1");
            tempID2 = Shader.PropertyToID("tempBlurRT2");
            cmd.GetTemporaryRT(tempID1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);
            cmd.GetTemporaryRT(tempID2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);

            tempRT1 = new RenderTargetIdentifier(tempID1);
            tempRT2 = new RenderTargetIdentifier(tempID2);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("KawaseBlur");

            //Copy the source into the first temp texture, applying brightness threshold
            cmd.SetGlobalFloat("_Threshold", threshold);
            cmd.SetGlobalFloat("_BrightnessMult", brightnessMult);
            cmd.Blit(SourceTexture, tempRT1, thresholdMaterial);

            for(int i = 0; i < passes - 1; i++)
            {
                //Copy between temp textures, blurring more each time
                cmd.SetGlobalFloat("_Offset", 0.5f + i);
                cmd.Blit(tempRT1, tempRT2, blurMaterial);

                RenderTargetIdentifier tempSwap = tempRT1;
                tempRT1 = tempRT2;
                tempRT2 = tempSwap;
            }

            //Final pass, outputting final blurred result
            cmd.SetGlobalFloat("_Offset", 0.5f + passes - 1);
            if(string.IsNullOrEmpty(outputTextureName))
            {
                cmd.Blit(tempRT1, SourceTexture, outputMaterial);
            }
            else
            {
                cmd.Blit(tempRT1, tempRT2, blurMaterial);
                cmd.SetGlobalTexture(outputTextureName, tempRT2);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }
}