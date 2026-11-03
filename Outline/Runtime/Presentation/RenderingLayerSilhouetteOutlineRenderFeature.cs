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
            private const int MAX_THICKNESS_PIXELS = 16;
            private const int ALLOCATION_ALIGNMENT_PIXELS = 32;

            private static readonly int SELECTION_MASK_ID = Shader.PropertyToID("_SelectionMask");
            private static readonly int DILATED_MASK_ID = Shader.PropertyToID("_HorizontallyDilatedMask");
            private static readonly int MASK_SIZE_PIXELS_ID = Shader.PropertyToID("_MaskSizePixels");
            private static readonly int MASK_ORIGIN_PIXELS_ID = Shader.PropertyToID("_MaskOriginPixels");
            private static readonly int MASK_CLIP_SCALE_OFFSET_ID = Shader.PropertyToID("_OutlineMaskClipScaleOffset");
            private static readonly int THICKNESS_PIXELS_ID = Shader.PropertyToID("_OutlineThicknessPixels");
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
                public Vector4 clipScaleOffset;
            }

            private sealed class FullscreenPassData
            {
                public TextureHandle selectionMask;
                public TextureHandle horizontallyDilatedMask;
                public Material material;
                public MaterialPropertyBlock properties;
                public RectInt rectanglePixels;
                public Vector4 clipScaleOffset;
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
                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                int thicknessPixels = Mathf.Clamp(Mathf.RoundToInt(_material.GetFloat(THICKNESS_PIXELS_ID)), 0, MAX_THICKNESS_PIXELS);
                if (thicknessPixels == 0)
                {
                    return;
                }

                GraphicsFormat maskFormat = GetSupportedMaskFormat();
                if (maskFormat == GraphicsFormat.None)
                {
                    return;
                }

                // Imported backbuffers do not expose a Render Graph texture descriptor.
                RenderTextureDescriptor cameraDescriptor = cameraData.cameraTargetDescriptor;
                if (resources.isActiveTargetBackBuffer)
                {
                    cameraDescriptor.width = cameraData.camera.pixelWidth;
                    cameraDescriptor.height = cameraData.camera.pixelHeight;
                }
                TextureDesc maskDescriptor = new(cameraDescriptor.width, cameraDescriptor.height)
                {
                    dimension = cameraDescriptor.dimension,
                    slices = cameraDescriptor.volumeDepth,
                    useDynamicScale = cameraDescriptor.useDynamicScale
                };
                bool isRenderingIntoTexture = !resources.isActiveTargetBackBuffer || cameraData.targetTexture != null;
                RectInt rectanglePixels = CalculateSelectionRectangle(cameraData, maskDescriptor.width, maskDescriptor.height,
                    thicknessPixels, isRenderingIntoTexture);
                if (rectanglePixels.width == 0 || rectanglePixels.height == 0)
                {
                    return;
                }

                Vector4 clipScaleOffset = OutlineScreenBounds.CalculateClipScaleOffset(rectanglePixels,
                    maskDescriptor.width, maskDescriptor.height, SystemInfo.graphicsUVStartsAtTop);
                maskDescriptor.width = rectanglePixels.width;
                maskDescriptor.height = rectanglePixels.height;
                maskDescriptor.sizeMode = TextureSizeMode.Explicit;
                maskDescriptor.name = "Selection silhouette and depth";
                maskDescriptor.colorFormat = maskFormat;
                maskDescriptor.msaaSamples = MSAASamples.None;
                maskDescriptor.bindTextureMS = false;
                maskDescriptor.filterMode = FilterMode.Point;
                maskDescriptor.clearBuffer = true;
                // -1 is empty; every hardware depth value in [0, 1] remains representable without packing.
                maskDescriptor.clearColor = new Color(-1f, -1f, -1f, -1f);
                TextureHandle selectionMask = renderGraph.CreateTexture(maskDescriptor);

                TextureDesc depthDescriptor = maskDescriptor;
                depthDescriptor.name = "Selection silhouette depth attachment";
                depthDescriptor.colorFormat = GraphicsFormat.None;
                depthDescriptor.depthBufferBits = DepthBits.Depth32;
                TextureHandle selectionDepth = renderGraph.CreateTexture(depthDescriptor);

                TextureDesc dilationDescriptor = maskDescriptor;
                dilationDescriptor.name = "Horizontally dilated silhouette";
                dilationDescriptor.clearBuffer = false;
                TextureHandle horizontalMask = renderGraph.CreateTexture(dilationDescriptor);

                RecordSolidMask(renderGraph, frameContext, selectionMask, selectionDepth, clipScaleOffset);
                RecordHorizontalDilation(renderGraph, selectionMask, horizontalMask);
                RecordComposite(renderGraph, resources, selectionMask, horizontalMask, rectanglePixels, clipScaleOffset);
            }

            #endregion

            #region Private Methods

            private RectInt CalculateSelectionRectangle(UniversalCameraData cameraData, int widthPixels, int heightPixels,
                int thicknessPixels, bool isRenderingIntoTexture)
            {
                // Stereo eyes and partial camera viewports need their own projection/viewport mapping.
                // Preserve their full-viewport path rather than cropping with a mono/full-camera projection.
                if (cameraData.camera.stereoEnabled || cameraData.camera.rect != new Rect(0f, 0f, 1f, 1f) ||
                    !RenderingLayerOutlineSelectionRegistry.Shared.TryGetSelectionBounds(_renderingLayerMask, out Bounds boundsMeters))
                {
                    return new RectInt(0, 0, widthPixels, heightPixels);
                }

                Matrix4x4 worldToClip = GL.GetGPUProjectionMatrix(cameraData.GetProjectionMatrix(), isRenderingIntoTexture) *
                    cameraData.GetViewMatrix();
                return OutlineScreenBounds.CalculatePixelRect(boundsMeters, worldToClip, widthPixels, heightPixels,
                    thicknessPixels + 2, SystemInfo.graphicsUVStartsAtTop, ALLOCATION_ALIGNMENT_PIXELS);
            }

            private GraphicsFormat GetSupportedMaskFormat()
            {
                if (SystemInfo.IsFormatSupported(GraphicsFormat.R32_SFloat, GraphicsFormatUsage.Render))
                {
                    return GraphicsFormat.R32_SFloat;
                }

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

            private void RecordSolidMask(RenderGraph renderGraph, ContextContainer frameContext, TextureHandle mask, TextureHandle depth,
                Vector4 clipScaleOffset)
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
                passData.clipScaleOffset = clipScaleOffset;
                builder.AllowGlobalStateModification(true);
                builder.UseRendererList(passData.rendererList);
                builder.SetRenderAttachment(mask, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(depth, AccessFlags.Write);
                builder.SetRenderFunc<MaskPassData>(DrawSolidMask);
            }

            private static void DrawSolidMask(MaskPassData data, RasterGraphContext context)
            {
                context.cmd.SetGlobalVector(MASK_CLIP_SCALE_OFFSET_ID, data.clipScaleOffset);
                context.cmd.DrawRendererList(data.rendererList);
                context.cmd.SetGlobalVector(MASK_CLIP_SCALE_OFFSET_ID, new Vector4(1f, 1f, 0f, 0f));
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

            private static void DrawHorizontalDilation(FullscreenPassData data, RasterGraphContext context)
            {
                ConfigureFullscreenProperties(data);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.material, HORIZONTAL_DILATION_PASS_INDEX,
                    MeshTopology.Triangles, 3, 1, data.properties);
            }

            private void RecordComposite(RenderGraph renderGraph, UniversalResourceData resources,
                TextureHandle mask, TextureHandle horizontalMask, RectInt rectanglePixels, Vector4 clipScaleOffset)
            {
                using IRasterRenderGraphBuilder builder = renderGraph.AddRasterRenderPass(
                    "Composite silhouette exterior", out FullscreenPassData passData);
                ConfigureFullscreenPass(passData, mask, horizontalMask);
                passData.rectanglePixels = rectanglePixels;
                passData.clipScaleOffset = clipScaleOffset;
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
                data.properties.SetVector(MASK_ORIGIN_PIXELS_ID, new Vector4(data.rectanglePixels.x, data.rectanglePixels.y, 0f, 0f));
                data.properties.SetVector(MASK_CLIP_SCALE_OFFSET_ID, data.clipScaleOffset);
                context.cmd.DrawProcedural(Matrix4x4.identity, data.material, COMPOSITE_PASS_INDEX,
                    MeshTopology.Triangles, 6, 1, data.properties);
            }

            private void ConfigureFullscreenPass(FullscreenPassData passData, TextureHandle mask, TextureHandle horizontalMask)
            {
                passData.selectionMask = mask;
                passData.horizontallyDilatedMask = horizontalMask;
                passData.material = _material;
                passData.properties = _fullscreenProperties;
            }

            private static void ConfigureFullscreenProperties(FullscreenPassData data)
            {
                RenderTexture mask = data.selectionMask;
                data.properties.Clear();
                data.properties.SetTexture(SELECTION_MASK_ID, mask);
                data.properties.SetVector(MASK_SIZE_PIXELS_ID, new Vector4(mask.width, mask.height, 0f, 0f));
            }

            #endregion
        }
    }
}
