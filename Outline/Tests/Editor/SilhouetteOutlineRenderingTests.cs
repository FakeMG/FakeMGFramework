using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace FakeMG.Outline.Tests
{
    public sealed class SilhouetteOutlineRenderingTests
    {
        private const string SHADER_GUID = "7235c96192b2e6e44be3a6b34a0607ae";
        private const int TEXTURE_SIZE_PIXELS = 64;
        private static readonly RectInt MASK_RECT_PIXELS = new(24, 24, 16, 16);

        private readonly List<Object> _temporaryObjects = new();
        private readonly List<RenderTexture> _renderTextures = new();
        private Material _material;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(AssetDatabase.GUIDToAssetPath(SHADER_GUID));
            Assert.That(shader, Is.Not.Null, "The silhouette shader GUID must still resolve after a move or rename.");
            Assert.That(shader.isSupported, Is.True);
            _material = TrackObject(new Material(shader));
            _material.SetColor("_OutlineColor", Color.yellow);
            _material.SetFloat("_OutlineThicknessPixels", 3f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (RenderTexture texture in _renderTextures)
            {
                texture.Release();
            }

            foreach (Object temporaryObject in _temporaryObjects)
            {
                Object.DestroyImmediate(temporaryObject);
            }

            _renderTextures.Clear();
            _temporaryObjects.Clear();
        }

        [Test]
        public void SolidMask_ProducesAnExteriorContour()
        {
            Texture2D mask = CreateMaskTexture(false);

            Texture2D result = RenderOutline(mask);

            Assert.That(IsYellow(result.GetPixel(23, 32)), Is.True);
            Assert.That(IsYellow(result.GetPixel(24, 32)), Is.False);
            Assert.That(IsYellow(result.GetPixel(20, 32)), Is.False);
        }

        [Test]
        public void InternalDepthDiscontinuities_DoNotProduceOutlinePixels()
        {
            Texture2D mask = CreateMaskTexture(true);

            Texture2D result = RenderOutline(mask);

            Assert.That(CountYellowPixels(result, MASK_RECT_PIXELS), Is.Zero);
            Assert.That(CountYellowPixels(result, new RectInt(0, 0, TEXTURE_SIZE_PIXELS, TEXTURE_SIZE_PIXELS)), Is.GreaterThan(0));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(3)]
        [TestCase(16)]
        public void Thickness_IsMeasuredInPixels(int thicknessPixels)
        {
            _material.SetFloat("_OutlineThicknessPixels", thicknessPixels);

            Texture2D result = RenderOutline(CreateMaskTexture(false));

            Assert.That(CountYellowPixels(result, new RectInt(0, 32, MASK_RECT_PIXELS.xMin, 1)), Is.EqualTo(thicknessPixels));
        }

        [Test]
        public void EmptySelection_DoesNotProduceOutlinePixels()
        {
            Texture2D mask = TrackObject(new Texture2D(TEXTURE_SIZE_PIXELS, TEXTURE_SIZE_PIXELS, TextureFormat.RGFloat, false, true));
            mask.SetPixels(new Color[TEXTURE_SIZE_PIXELS * TEXTURE_SIZE_PIXELS]);
            mask.Apply();

            Texture2D result = RenderOutline(mask);

            Assert.That(CountYellowPixels(result, new RectInt(0, 0, TEXTURE_SIZE_PIXELS, TEXTURE_SIZE_PIXELS)), Is.Zero);
        }

        [Test]
        public void SceneOccluder_HidesTheContourWithoutChangingTheSolidMask()
        {
            Texture2D mask = CreateMaskTexture(false);

            Texture2D result = RenderOutline(mask, sceneClearDepth01: 0f);

            Assert.That(CountYellowPixels(result, new RectInt(0, 0, TEXTURE_SIZE_PIXELS, TEXTURE_SIZE_PIXELS)), Is.Zero);
            Assert.That(mask.GetPixel(32, 32).r, Is.EqualTo(1f));
        }

        [Test]
        public void BodySubmeshesAndSeparateWheels_CombineWithoutInteriorContours()
        {
            Mesh body = CreateBodyMesh();
            Mesh wheel = CreateQuadMesh(new Vector2(-0.4f, -0.6f), new Vector2(-0.2f, -0.4f));
            RenderTexture mask = CreateRenderTexture(RenderTextureFormat.RGFloat, 24);
            using (var commands = new CommandBuffer())
            {
                commands.SetRenderTarget(mask);
                commands.ClearRenderTarget(true, true, Color.clear);
                commands.SetViewProjectionMatrices(Matrix4x4.identity,
                    GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(-1f, 1f, -1f, 1f, 0.1f, 10f), true));
                commands.DrawMesh(body, Matrix4x4.identity, _material, 0, GetPassIndex("SolidSelectionMask"));
                commands.DrawMesh(body, Matrix4x4.identity, _material, 1, GetPassIndex("SolidSelectionMask"));
                commands.DrawMesh(wheel, Matrix4x4.identity, _material, 0, GetPassIndex("SolidSelectionMask"));
                commands.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity);
                Graphics.ExecuteCommandBuffer(commands);
            }

            Texture2D coverage = ReadTexture(mask);
            Texture2D result = RenderOutline(mask);

            Assert.That(coverage.GetPixel(20, 32).r, Is.EqualTo(1f), "First body submesh must contribute.");
            Assert.That(coverage.GetPixel(44, 32).r, Is.EqualTo(1f), "Second body submesh must contribute.");
            int coveragePixelCount = 0;
            for (int yPixels = 0; yPixels < TEXTURE_SIZE_PIXELS; yPixels++)
            {
                for (int xPixels = 0; xPixels < TEXTURE_SIZE_PIXELS; xPixels++)
                {
                    if (coverage.GetPixel(xPixels, yPixels).r > 0.5f)
                    {
                        coveragePixelCount++;
                        Assert.That(IsYellow(result.GetPixel(xPixels, yPixels)), Is.False, "Covered geometry must never become an internal contour.");
                    }
                }
            }

            Assert.That(coveragePixelCount, Is.GreaterThan(32 * 32), "The separate wheel must extend the combined body mask.");
        }

        #endregion

        #region Private Methods

        private Texture2D CreateMaskTexture(bool hasDepthVariation)
        {
            Texture2D mask = TrackObject(new Texture2D(TEXTURE_SIZE_PIXELS, TEXTURE_SIZE_PIXELS, TextureFormat.RGFloat, false, true));
            var pixels = new Color[TEXTURE_SIZE_PIXELS * TEXTURE_SIZE_PIXELS];
            for (int yPixels = MASK_RECT_PIXELS.yMin; yPixels < MASK_RECT_PIXELS.yMax; yPixels++)
            {
                for (int xPixels = MASK_RECT_PIXELS.xMin; xPixels < MASK_RECT_PIXELS.xMax; xPixels++)
                {
                    float depth01 = hasDepthVariation && xPixels % 2 == 0 ? 0.75f : 0.5f;
                    pixels[yPixels * TEXTURE_SIZE_PIXELS + xPixels] = new Color(1f, depth01, 0f, 0f);
                }
            }

            mask.SetPixels(pixels);
            mask.Apply();
            return mask;
        }

        private Texture2D RenderOutline(Texture mask, float sceneClearDepth01 = 1f)
        {
            RenderTexture horizontalMask = CreateRenderTexture(RenderTextureFormat.RGFloat, 0);
            RenderTexture result = CreateRenderTexture(RenderTextureFormat.ARGB32, 24);
            var properties = new MaterialPropertyBlock();
            properties.SetTexture("_SelectionMask", mask);
            properties.SetTexture("_HorizontallyDilatedMask", horizontalMask);
            properties.SetVector("_MaskSizePixels", new Vector4(TEXTURE_SIZE_PIXELS, TEXTURE_SIZE_PIXELS, 0f, 0f));
            using (var commands = new CommandBuffer())
            {
                commands.SetRenderTarget(horizontalMask);
                commands.DrawProcedural(Matrix4x4.identity, _material, GetPassIndex("HorizontalMaskDilation"),
                    MeshTopology.Triangles, 3, 1, properties);
                commands.SetRenderTarget(result);
                commands.ClearRenderTarget(true, true, Color.black, sceneClearDepth01);
                commands.DrawProcedural(Matrix4x4.identity, _material, GetPassIndex("CompositeExteriorOutline"),
                    MeshTopology.Triangles, 3, 1, properties);
                Graphics.ExecuteCommandBuffer(commands);
            }

            return ReadTexture(result);
        }

        private RenderTexture CreateRenderTexture(RenderTextureFormat format, int depthBits)
        {
            RenderTexture texture = TrackObject(new RenderTexture(TEXTURE_SIZE_PIXELS, TEXTURE_SIZE_PIXELS, depthBits, format));
            Assert.That(texture.Create(), Is.True);
            _renderTextures.Add(texture);
            return texture;
        }

        private int GetPassIndex(string passName)
        {
            int passIndex = _material.FindPass(passName);
            Assert.That(passIndex, Is.GreaterThanOrEqualTo(0), $"The shader must contain the public pass '{passName}'.");
            return passIndex;
        }

        private Texture2D ReadTexture(RenderTexture source)
        {
            RenderTexture previousTarget = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                Texture2D result = TrackObject(new Texture2D(TEXTURE_SIZE_PIXELS, TEXTURE_SIZE_PIXELS, TextureFormat.RGBAFloat, false, true));
                result.ReadPixels(new Rect(0f, 0f, TEXTURE_SIZE_PIXELS, TEXTURE_SIZE_PIXELS), 0, 0);
                result.Apply();
                return result;
            }
            finally
            {
                RenderTexture.active = previousTarget;
            }
        }

        private static int CountYellowPixels(Texture2D texture, RectInt rectanglePixels)
        {
            int yellowPixelCount = 0;
            for (int yPixels = rectanglePixels.yMin; yPixels < rectanglePixels.yMax; yPixels++)
            {
                for (int xPixels = rectanglePixels.xMin; xPixels < rectanglePixels.xMax; xPixels++)
                {
                    if (IsYellow(texture.GetPixel(xPixels, yPixels)))
                    {
                        yellowPixelCount++;
                    }
                }
            }

            return yellowPixelCount;
        }

        private static bool IsYellow(Color color)
        {
            return color.r > 0.9f && color.g > 0.9f && color.b < 0.1f;
        }

        private Mesh CreateBodyMesh()
        {
            Mesh body = TrackObject(new Mesh());
            body.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, -1f), new Vector3(-0.5f, 0.5f, -1f),
                new Vector3(0f, 0.5f, -1f), new Vector3(0f, -0.5f, -1f),
                new Vector3(0f, -0.5f, -1f), new Vector3(0f, 0.5f, -1f),
                new Vector3(0.5f, 0.5f, -1f), new Vector3(0.5f, -0.5f, -1f)
            };
            body.subMeshCount = 2;
            body.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            body.SetTriangles(new[] { 4, 5, 6, 4, 6, 7 }, 1);
            return body;
        }

        private Mesh CreateQuadMesh(Vector2 minimumPositionMeters, Vector2 maximumPositionMeters)
        {
            Mesh quad = TrackObject(new Mesh());
            quad.vertices = new[]
            {
                new Vector3(minimumPositionMeters.x, minimumPositionMeters.y, -1f),
                new Vector3(minimumPositionMeters.x, maximumPositionMeters.y, -1f),
                new Vector3(maximumPositionMeters.x, maximumPositionMeters.y, -1f),
                new Vector3(maximumPositionMeters.x, minimumPositionMeters.y, -1f)
            };
            quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            return quad;
        }

        private T TrackObject<T>(T temporaryObject) where T : Object
        {
            _temporaryObjects.Add(temporaryObject);
            return temporaryObject;
        }

        #endregion
    }
}
