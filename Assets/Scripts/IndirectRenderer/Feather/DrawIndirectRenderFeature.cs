using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace JustEngine.JustGraphcis
{
    public class DrawIndirectRenderFeature : ScriptableRendererFeature
    {
        [SerializeField]
        public bool cullingHZ;

        private HierarchicalZBufferRenderPass m_ZBufferPass;
        private DrawIndirectRenderPass m_DrawIndirectPass;
        public override void Create()
        {
            m_ZBufferPass = new HierarchicalZBufferRenderPass();
            m_DrawIndirectPass = new DrawIndirectRenderPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            Shader.SetGlobalInt(HZShaderLibrary.HZCullingEnable, cullingHZ ? 1 : 0);

            if (cullingHZ)
            {
                renderer.EnqueuePass(m_ZBufferPass);
            }
 

            if (m_DrawIndirectPass.Setup())
                renderer.EnqueuePass(m_DrawIndirectPass);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            m_ZBufferPass.Release();
        }
    }
    public class HierarchicalZBufferRenderPass : ScriptableRenderPass
    {
        // Consts
        private const int MAXIMUM_BUFFER_SIZE = 1024;

        private RenderTexture depthZBufferTexture;
        private Material material;
        public HierarchicalZBufferRenderPass()
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
            material = new Material(Shader.Find("Just/Urp/HierarchicalZBuffer"));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {

            //Create Texture
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;
            ref var descriptor = ref renderingData.cameraData.cameraTargetDescriptor;
            int size = (int)Mathf.Max(descriptor.width, descriptor.height);
            size = (int)Mathf.Min((float)Mathf.NextPowerOfTwo(size), (float)MAXIMUM_BUFFER_SIZE);
            Shader.SetGlobalFloat(HZShaderLibrary.HZTextureSize, size);

            CreateDepthBufferTextureIfNeed(size);
            var depthZBufferIdentifier = new RenderTargetIdentifier(depthZBufferTexture);
            var cmd = CommandBufferPool.Get();

            cmd.BeginSample(HZShaderLibrary.HClipDrawName);
            //Copy Depth To Buffer
            cmd.Blit(null, depthZBufferIdentifier, material, 0);
            int index = 0;
            while (size > 8)
            {
                int temporariesId = GetTemporariesTextureId(index);
                int prevId = GetTemporariesTextureId(index - 1);
                size >>= 1;
                size = Mathf.Max(size, 1);
                cmd.GetTemporaryRT(temporariesId, size, size, 0, FilterMode.Point, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
                if (index == 0)
                {
                    cmd.SetGlobalTexture(HZShaderLibrary.HZMainTexName, depthZBufferIdentifier);
                    cmd.Blit(depthZBufferIdentifier, temporariesId, material, 1);
                }
                else
                {
                    cmd.SetGlobalTexture(HZShaderLibrary.HZMainTexName, prevId);
                    cmd.Blit(prevId, temporariesId, material, 1);
                }
                cmd.CopyTexture(temporariesId, 0, 0, depthZBufferIdentifier, 0, index + 1);
                if (index > 0)
                {
                    cmd.ReleaseTemporaryRT(prevId);
                }
                index++;
            }

            var lastId = GetTemporariesTextureId(index - 1);
            if (lastId != -1) cmd.ReleaseTemporaryRT(lastId);
            cmd.EndSample(HZShaderLibrary.HClipDrawName);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private static List<int> tempoprariesIds = new List<int>();
        int GetTemporariesTextureId(int index)
        {
            if (index < 0) return -1;
            while (index >= tempoprariesIds.Count)
            {
                tempoprariesIds.Add(Shader.PropertyToID("Temporaries HZBuffer Texture" + index));
            }
            return tempoprariesIds[index];
        }

        public void CreateDepthBufferTextureIfNeed(int size)
        {
            if (depthZBufferTexture != null && (depthZBufferTexture.width != size || depthZBufferTexture.height != size))
            {
                depthZBufferTexture.Release();
                depthZBufferTexture = null;
            }

            if (depthZBufferTexture == null)
            {
                depthZBufferTexture = new RenderTexture(size, size, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
                depthZBufferTexture.filterMode = FilterMode.Point;
                depthZBufferTexture.useMipMap = true;
                depthZBufferTexture.autoGenerateMips = false;
                depthZBufferTexture.name = "Hierarchical Buffer";
                depthZBufferTexture.Create();
                depthZBufferTexture.hideFlags = HideFlags.HideAndDontSave;
                Shader.SetGlobalTexture("_HiZTextureTex", depthZBufferTexture);
            }

        }

        public void Release()
        {
            tempoprariesIds.Clear();
            if (depthZBufferTexture)
            {
                depthZBufferTexture.Release();
                depthZBufferTexture = null;
            }
            ReleaseMaterial();
        }

        void ReleaseMaterial()
        {
            if (material)
            {
                GameObject.DestroyImmediate(material);
            }
        }


    }

    public class DrawIndirectRenderPass : ScriptableRenderPass
    {
        public DrawIndirectRenderPass()
        {
            this.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        public bool Setup()
        {
            return IndirectRenderStack.GetCount() > 0;
        }
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
            cmd.BeginSample(HZShaderLibrary.SampleDrawName);
            int count = IndirectRenderStack.GetCount();
            for (int i = 0; i < count; i++)
            {
                var renderer = IndirectRenderStack.GetRenderer(i);
                if (!renderer.drawBySelf)
                {
                    renderer.CallRender(cmd);
                }
            }
            cmd.EndSample(HZShaderLibrary.SampleDrawName);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public static class HZShaderLibrary
    {
        public static int HZTextureSize = Shader.PropertyToID("_HiZTextureSize");
        public static int HZTexture = Shader.PropertyToID("_HiZTextureTex");
        public static int HZMainTexName = Shader.PropertyToID("_MainTex");
        public static int HZCullingEnable = Shader.PropertyToID("_HZCullingEnable");

        public const string HClipDrawName = "HZ Clling Buffer";
        public const string SampleDrawName = "Draw Indirect Render";
    }

    public static class IndirectRenderStack
    {
        private static readonly List<IndirectRenderer> renderers = new List<IndirectRenderer>();
        public static int CullingHZRendererCount;

        public static void Register(IndirectRenderer renderer)
        {
            renderers.Add(renderer);
        }

        public static void UnRegister(IndirectRenderer renderer)
        {
            renderers.Remove(renderer);
        }

        internal static int GetCount()
        {
            return renderers.Count;
        }

        internal static IndirectRenderer GetRenderer(int index)
        {
            return renderers[index];
        }
    }
}