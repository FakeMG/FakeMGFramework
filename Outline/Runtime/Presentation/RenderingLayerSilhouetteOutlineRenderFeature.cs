using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace FakeMG.Outline
{
    public sealed class RenderingLayerSilhouetteOutlineRenderFeature : ScriptableRendererFeature
    {
        [Header("Selection")]
        [SerializeField] private RenderingLayerMask _renderingLayerMask;
        [SerializeField] private LayerMask _gameObjectLayerMask = ~0;

        [Header("Appearance")]
        [Tooltip("Silhouette material containing the solid mask, dilation, and composite passes. Edit its color and pixel thickness.")]
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private bool _showInSceneView = true;

        private SilhouetteOutlinePass _pass;

        #region Public Methods

        public override void Create()
        {
            _pass = new SilhouetteOutlinePass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(_renderingLayerMask))
            {
                return;
            }

            CameraType cameraType = renderingData.cameraData.cameraType;
            if (cameraType != CameraType.Game && (cameraType != CameraType.SceneView || !_showInSceneView))
            {
                return;
            }

            _pass.ConfigureSelection(_outlineMaterial, _gameObjectLayerMask, _renderingLayerMask);
            renderer.EnqueuePass(_pass);
        }

        #endregion

        private sealed class SilhouetteOutlinePass : ScriptableRenderPass
        {
            private const int SOLID_MASK_PASS_INDEX = 0;
            private const int HORIZONTAL_DILATION_PASS_INDEX = 1;
            private const int COMPOSITE_PASS_INDEX = 2;

            private static readonly int SELECTION_MASK_ID = Shader.PropertyToID("_SelectionMask");
            private static readonly int DILATED_MASK_ID = Shader.PropertyToID("_HorizontallyDilatedMask");
            private static readonly int MASK_SIZE_PIXELS_ID = Shader.PropertyToID("_MaskSizePixels");
            private static readonly List<ShaderTagId> SHADER_TAGS = new()
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit")
            };

            private readonly MaterialPropertyBlock _fullscreenProperties = new();
            private Material _material;
            private LayerMask _gameObjectLayerMask;
            private uint _renderingLayerMask;
            private bool _hasReportedUnsupportedMaskFormat;

            private sealed class MaskPassData
            {
                public RendererListHandle rendererList;
            }

            private sealed class FullscreenPassData
            {
                public TextureHandle selectionMask;
                public TextureHandle horizontallyDilatedMask;
                public Material material;
                public MaterialPropertyBlock properties;
            }

            public SilhouetteOutlinePass()
            {
                // Transparent occluders with depth writes are included in the final hardware depth test.
                renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            }

            #region Public Methods

            public void ConfigureSelection(Material material, LayerMask gameObjectLayerMask, uint renderingLayerMask)
            {
                _material = material;
                _gameObjectLayerMask = gameObjectLayerMask;
                _renderingLayerMask = renderingLayerMask;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
            {
                UniversalResourceData resources = frameContext.Get<UniversalResourceData>();
                GraphicsFormat maskFormat = GetSupportedMaskFormat();
                if (maskFormat == GraphicsFormat.None)
                {
                    return;
                }

                TextureDesc maskDescriptor = renderGraph.GetTextureDesc(resources.activeColorTexture);
                maskDescriptor.name = "Selection silhouette and depth";
                maskDescriptor.colorFormat = maskFormat;
                maskDescriptor.msaaSamples = MSAASamples.None;
                maskDescriptor.bindTextureMS = false;
                maskDescriptor.filterMode = FilterMode.Point;
                maskDescriptor.clearBuffer = true;
                maskDescriptor.clearColor = Color.clear;
                TextureHandle selectionMask = renderGraph.CreateTexture(maskDescriptor);

                TextureDesc depthDescriptor = maskDescriptor;
                depthDescriptor.name = "Selection silhouette depth attachment";
                depthDescriptor.depthBufferBits = DepthBits.Depth32;
                TextureHandle selectionDepth = renderGraph.CreateTexture(depthDescriptor);

                TextureDesc dilationDescriptor = maskDescriptor;
                dilationDescriptor.name = "Horizontally dilated silhouette";
                dilationDescriptor.clearBuffer = false;
                TextureHandle horizontalMask = renderGraph.CreateTexture(dilationDescriptor);

                RecordSolidMask(renderGraph, frameContext, selectionMask, selectionDepth);
                RecordHorizontalDilation(renderGraph, selectionMask, horizontalMask);
                RecordComposite(renderGraph, resources, selectionMask, horizontalMask);
            }

            #endregion

            #region Private Methods

            private GraphicsFormat GetSupportedMaskFormat()
            {
                if (SystemInfo.IsFormatSupported(GraphicsFormat.R32G32_SFloat, GraphicsFormatUsage.Render))
                {
                    return GraphicsFormat.R32G32_SFloat;
                }

                if (SystemInfo.IsFormatSupported(GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormatUsage.Render))
                {
                    return GraphicsFormat.R32G32B32A32_SFloat;
                }

                if (!_hasReportedUnsupportedMaskFormat)
                {
                    Echo.Error("Silhouette outlines are unavailable because this device cannot render a full-precision mask and depth texture.");
                    _hasReportedUnsupportedMaskFormat = true;
                }

                return GraphicsFormat.None;
            }

            private void RecordSolidMask(RenderGraph renderGraph, ContextContainer frameContext, TextureHandle mask, TextureHandle depth)
            {
                UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                UniversalLightData lightData = frameContext.Get<UniversalLightData>();
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    SHADER_TAGS, renderingData, cameraData, lightData, SortingCriteria.CommonOpaque);
                drawingSettings.overrideMaterial = _material;
                drawingSettings.overrideMaterialPassIndex = SOLID_MASK_PASS_INDEX;
                FilteringSettings filteringSettings = new(RenderQueueRange.all, _gameObjectLayerMask, _renderingLayerMask);
                RendererListParams rendererListParameters = new(renderingData.cullResults, drawingSettings, filteringSettings);

                using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(
                    "Solid selection silhouette", out MaskPassData passData);
                passData.rendererList = renderGraph.CreateRendererList(rendererListParameters);
                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                builder.SetRenderFunc<MaskPassData>(DrawSolidMask);
            }

            private static void DrawSolidMask(MaskPassData data, RasterGraphContext context)
            {
                context.cmd.DrawRendererList(data.rendererList);
            }

            private void RecordHorizontalDilation(RenderGraph renderGraph, TextureHandle mask, TextureHandle horizontalMask)
            {
                using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(
                    "Dilate silhouette horizontally", out FullscreenPassData passData);
                ConfigureFullscreenPass(passData, mask, horizontalMask);
                builder.UseTexture(mask, AccessFlags.Read);
                builder.SetRenderAttachment(horizontalMask, 0, AccessFlags.Write);
                builder.SetRenderFunc<FullscreenPassData>(DrawHorizontalDilation);
            }

            private void ConfigureFullscreenPass(FullscreenPassData passData, TextureHandle mask, TextureHandle horizontalMask)
            {
                passData.selectionMask = mask;
                passData.horizontallyDilatedMask = horizontalMask;
                passData.material = _material;
                passData.properties = _fullscreenProperties;
            }

            private static void DrawHorizontalDilation(FullscreenPassData data, RasterGraphContext context)
            {
                ConfigureFullscreenProperties(data);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.material, HORIZONTAL_DILATION_PASS_INDEX,
                    MeshTopology.Triangles, 3, 1, data.properties);
            }

            private static void ConfigureFullscreenProperties(FullscreenPassData data)
            {
                RenderTexture mask = data.selectionMask;
                data.properties.Clear();
                data.properties.SetTexture(SELECTION_MASK_ID, mask);
                data.properties.SetVector(MASK_SIZE_PIXELS_ID, new Vector4(mask.width, mask.height, 0f, 0f));
            }

            private void RecordComposite(RenderGraph renderGraph, UniversalResourceData resources,
                TextureHandle mask, TextureHandle horizontalMask)
            {
                using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(
                    "Composite silhouette exterior", out FullscreenPassData passData);
                ConfigureFullscreenPass(passData, mask, horizontalMask);
                builder.UseTexture(mask, AccessFlags.Read);
                builder.UseTexture(horizontalMask, AccessFlags.Read);
                builder.SetRenderAttachment(resources.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture, AccessFlags.Read);
                builder.SetRenderFunc<FullscreenPassData>(DrawComposite);
            }

            private static void DrawComposite(FullscreenPassData data, RasterGraphContext context)
            {
                ConfigureFullscreenProperties(data);
                data.properties.SetTexture(DILATED_MASK_ID, data.horizontallyDilatedMask);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.material, COMPOSITE_PASS_INDEX,
                    MeshTopology.Triangles, 3, 1, data.properties);
            }

            #endregion
        }
    }
}
