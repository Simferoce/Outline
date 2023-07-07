using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DisturberFeature : ScriptableRendererFeature
{
    public class DisturberPass : ScriptableRenderPass
    {
        public string profilerTag;

        private ScriptableRenderer renderer;
        private RenderTargetHandle textureHandle;
        private List<MeshRenderer> renderers = new List<MeshRenderer>();
        private CustomCameraSettings cameraSettings = null;

        public DisturberPass(string profilerTag, RenderPassEvent renderPassEvent)
        {
            this.profilerTag = profilerTag;
            base.renderPassEvent = renderPassEvent;
        }

        public void Setup(ScriptableRenderer renderer, CustomCameraSettings cameraSettings, List<MeshRenderer> renderers)
        {
            this.renderer = renderer;
            this.renderers = renderers;
            this.cameraSettings = cameraSettings;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(textureHandle.id, cameraTextureDescriptor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            cmd.Clear();

            CameraData cameraData = renderingData.cameraData;
            if (cameraSettings.overrideCamera)
            {
                Matrix4x4 proj = Matrix4x4.Perspective(cameraSettings.cameraFieldOfView, cameraData.camera.aspect, 0.3f, 1000);
                proj = GL.GetGPUProjectionMatrix(proj, cameraData.IsCameraProjectionMatrixFlipped());
                Matrix4x4 viewMatrix = cameraData.GetViewMatrix();
                Vector4 column = viewMatrix.GetColumn(3);
                viewMatrix.SetColumn(3, column + cameraSettings.offset);
                RenderingUtils.SetViewAndProjectionMatrices(cmd, viewMatrix, proj, setInverseMatrices: false);
            }

            foreach (MeshRenderer renderer in renderers)
            {
                cmd.DrawMesh(renderer.GetComponent<MeshFilter>().sharedMesh, renderer.localToWorldMatrix, renderer.sharedMaterial);
            }

            Graphics.ExecuteCommandBuffer(cmd);

            //cmd.SetGlobalTexture("_DisturberTexture", textureHandle.Identifier());
            //context.ExecuteCommandBuffer(cmd);

            if (cameraSettings.overrideCamera && cameraSettings.restoreCamera && !cameraData.xr.enabled)
            {
                RenderingUtils.SetViewAndProjectionMatrices(cmd, cameraData.GetViewMatrix(), cameraData.GetGPUProjectionMatrix(), setInverseMatrices: false);
            }

            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(textureHandle.id);
        }
    }

    [Serializable]
    public class CustomCameraSettings
    {
        public bool overrideCamera = false;

        public bool restoreCamera = true;

        public Vector4 offset;

        public float cameraFieldOfView = 60f;
    }

    [System.Serializable]
    public class Settings
    {
        public RenderPassEvent WhenToInsert = RenderPassEvent.AfterRendering;
        public Transform source = null;
        public CustomCameraSettings cameraSettings = new CustomCameraSettings();
    }

    [SerializeField]
    public Settings settings = new Settings();

    private DisturberPass disturberPass;

    public override void Create()
    {
        disturberPass = new DisturberPass("Disturber Render", settings.WhenToInsert);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        disturberPass.Setup(renderer, settings.cameraSettings, GameObject.FindGameObjectsWithTag("Disturber").Select(x => x.GetComponent<MeshRenderer>()).Where(x => x != null).ToList());

        renderer.EnqueuePass(disturberPass);
    }
}

