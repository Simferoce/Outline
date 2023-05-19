using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurBufferFeature : ScriptableRendererFeature
{
    public class BlurPass : ScriptableRenderPass
    {
        public string profilerTag;

        private Material material;
        private ScriptableRenderer renderer;
        private RenderTargetHandle blurHandle;
        private float[] guassianKernel = new float[100];
        private int guassianKernelSize;

        public BlurPass(string profilerTag, RenderPassEvent renderPassEvent, Material material)
        {
            this.profilerTag = profilerTag;
            this.renderPassEvent = renderPassEvent;
            this.material = material;
        }

        public void Setup(ScriptableRenderer renderer, List<float> guassianKernel, int guassianKernelSize)
        {
            this.renderer = renderer;
            for (int i = 0; i < guassianKernel.Count; ++i)
            {
                this.guassianKernel[i] = guassianKernel[i];
            }
            this.guassianKernelSize = guassianKernelSize;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(blurHandle.id, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            cmd.Clear();

            cmd.SetGlobalInt("_GuassianKernel_Size", guassianKernelSize);
            cmd.SetGlobalFloatArray("_GuassianKernel", guassianKernel);
            cmd.Blit(renderer.cameraColorTarget, blurHandle.Identifier(), material, 0);
            //cmd.Blit(blurHandle.Identifier(), renderer.cameraColorTarget, material, 0);
            cmd.SetGlobalTexture("_CameraBlurTexture", blurHandle.Identifier());

            context.ExecuteCommandBuffer(cmd);

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(blurHandle.id);
        }
    }


    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
        public Material Material;

        [Range(1, 10)] public int size = 1;
        public float strength = 0.8f;
    }

    [SerializeField]
    public Settings settings = new Settings();

    private BlurPass blurPass;
    private List<float> guassianKernel = new List<float>();

    private float Guass(float sigma, float x, float y)
    {
        float twoSigmaSqr = 2 * sigma * sigma;
        return 1 / Mathf.Sqrt(twoSigmaSqr * Mathf.PI) * Mathf.Exp(-(x * x + y * y) / twoSigmaSqr);
    }

    private void GuassianKernel(int size, float sigma, List<float> values)
    {
        for (int x = 0; x < size; ++x)
        {
            for (int y = 0; y < size; ++y)
            {
                values.Add(Guass(sigma, x, y));
            }
        }
    }

    public override void Create()
    {
        blurPass = new BlurPass("Blur", settings.WhenToInsert, settings.Material);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        guassianKernel.Clear();
        GuassianKernel(settings.size, settings.strength, guassianKernel);
        blurPass.Setup(renderer, guassianKernel, settings.size);

        renderer.EnqueuePass(blurPass);
    }
}

