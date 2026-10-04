using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace FakeMG.Outline
{
    /// <summary>
    /// Renders objects assigned to configured rendering layers with an outline material.
    /// </summary>
    public sealed class RenderingLayerOutlineRenderFeature : ScriptableRendererFeature
    {
        [Serializable]
        public sealed class Settings
        {
            [Tooltip("When the pass executes.")]
            public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

            [Tooltip("Optional additional filter using normal GameObject layers.")]
            public LayerMask gameObjectLayerMask = ~0;

            [Tooltip("Filter using Renderer.renderingLayerMask.")]
            public RenderingLayerMask renderingLayerMask = RenderingLayerMask.defaultRenderingLayerMask;

            [Tooltip("Which material render queues should be included.")]
            public RenderQueueType renderQueue = RenderQueueType.Opaque;

            [Tooltip("Material used to redraw matching objects.")]
            public Material overrideMaterial;

            [Min(0)]
            [Tooltip("Shader pass index in the override material.")]
            public int overrideMaterialPassIndex;

            [Tooltip("Include the Scene view camera.")]
            public bool showInSceneView = true;

            [Tooltip("Include preview and inspector cameras.")]
            public bool showInPreviewCameras;
        }

        public enum RenderQueueType
        {
            Opaque,
            Transparent,
            All
        }

        [SerializeField] private Settings settings = new();

        private RenderingLayerPass renderPass;

        public override void Create()
        {
            renderPass = new RenderingLayerPass(settings)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (settings.overrideMaterial == null)
                return;

            CameraType cameraType = renderingData.cameraData.cameraType;

            if (!settings.showInSceneView && cameraType == CameraType.SceneView)
            {
                return;
            }

            if (!settings.showInPreviewCameras && cameraType == CameraType.Preview)
            {
                return;
            }

            renderPass.UpdateSettings(settings);
            renderer.EnqueuePass(renderPass);
        }

        private sealed class RenderingLayerPass : ScriptableRenderPass
        {
            private static readonly List<ShaderTagId> ShaderTags = new()
        {
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit")
        };

            private Settings settings;

            private sealed class PassData
            {
                public RendererListHandle rendererList;
            }

            public RenderingLayerPass(Settings settings)
            {
                this.settings = settings;
            }

            public void UpdateSettings(Settings newSettings)
            {
                settings = newSettings;
                renderPassEvent = newSettings.renderPassEvent;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                if (settings.overrideMaterial == null)
                    return;

                UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();

                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();

                UniversalLightData lightData = frameContext.Get<UniversalLightData>();

                UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();

                RenderQueueRange queueRange = GetRenderQueueRange(settings.renderQueue);

                uint renderingLayerMask = settings.renderingLayerMask;

                // Both filters are applied:
                // GameObject.layer AND Renderer.renderingLayerMask.
                FilteringSettings filteringSettings = new(queueRange, settings.gameObjectLayerMask, renderingLayerMask);

                SortingCriteria sortingCriteria =
                    settings.renderQueue == RenderQueueType.Transparent
                        ? SortingCriteria.CommonTransparent
                        : cameraData.defaultOpaqueSortFlags;

                DrawingSettings drawingSettings =
                    RenderingUtils.CreateDrawingSettings(
                        ShaderTags,
                        renderingData,
                        cameraData,
                        lightData,
                        sortingCriteria);

                drawingSettings.overrideMaterial = settings.overrideMaterial;

                drawingSettings.overrideMaterialPassIndex = settings.overrideMaterialPassIndex;

                RendererListParams rendererListParams = new(renderingData.cullResults, drawingSettings, filteringSettings);

                using IRasterRenderGraphBuilder builder =
                    renderGraph.AddRasterRenderPass(
                        "Rendering Layer Filter Pass",
                        out PassData passData);

                passData.rendererList = renderGraph.CreateRendererList(rendererListParams);

                builder.UseRendererList(passData.rendererList);

                // Draw over the existing camera color.
                builder.SetRenderAttachment(
                    resourceData.activeColorTexture,
                    0,
                    AccessFlags.Write);

                // Use the existing camera depth for occlusion.
                builder.SetRenderAttachmentDepth(
                    resourceData.activeDepthTexture,
                    AccessFlags.Read);

                builder.SetRenderFunc(
                    static (PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.DrawRendererList(
                            data.rendererList);
                    });
            }

            private static RenderQueueRange GetRenderQueueRange(RenderQueueType queueType)
            {
                return queueType switch
                {
                    RenderQueueType.Opaque => RenderQueueRange.opaque,
                    RenderQueueType.Transparent => RenderQueueRange.transparent,
                    _ => RenderQueueRange.all
                };
            }
        }
    }
}
