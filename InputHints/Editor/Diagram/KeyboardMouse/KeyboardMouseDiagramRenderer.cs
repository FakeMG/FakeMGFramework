using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FakeMG.InputHints.Editor
{
    /// <summary>
    /// Draws <see cref="KeyboardMouseDiagramLayout"/> as white outlines and labels on transparency, like the gamepad diagrams,
    /// so one tint and the traced fill masks serve every device.
    /// </summary>
    internal static class KeyboardMouseDiagramRenderer
    {
        private const float SINGLE_CHARACTER_FONT_SIZE_KEY_UNITS = 0.4f;
        private const float WORD_FONT_SIZE_KEY_UNITS = 0.28f;

        /// <remarks>Keeps a long label such as 'PrtSc' clear of the key's rim.</remarks>
        private const float LABEL_MARGIN_KEY_UNITS = 0.18f;
        private const int KEY_SLICE_BORDER_PIXELS = 14;
        private const float MOUSE_ASPECT_TOLERANCE = 0.02f;

        /// <remarks>The blanks draw a dark rim around a light face. Only the rim is kept, fading out across this luminance range.</remarks>
        private const float RIM_FULL_BELOW_LUMINANCE01 = 0.3f;
        private const float RIM_GONE_ABOVE_LUMINANCE01 = 0.6f;

        #region Public Methods

        /// <exception cref="InvalidOperationException">A blank is missing, or the mouse blank no longer fits the layout.</exception>
        public static void Render(string keyBlankPath, string mouseBlankPath, string diagramPath)
        {
            Texture2D keyRim = LoadRim(keyBlankPath);
            Texture2D mouseRim = LoadRim(mouseBlankPath);
            ThrowIfMouseDoesNotFit(mouseRim, mouseBlankPath);

            Sprite keySprite = Sprite.Create(
                keyRim,
                new Rect(0f, 0f, keyRim.width, keyRim.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                Vector4.one * KEY_SLICE_BORDER_PIXELS);
            Sprite mouseSprite = Sprite.Create(mouseRim, new Rect(0f, 0f, mouseRim.width, mouseRim.height), new Vector2(0.5f, 0.5f));
            Vector2Int sizePixels = KeyboardMouseDiagramLayout.SizePixels;
            var renderTexture = new RenderTexture(sizePixels.x, sizePixels.y, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Scene scene = EditorSceneManager.NewPreviewScene();
            Texture2D diagram = null;
            try
            {
                Camera camera = CreateCamera(scene, renderTexture);
                RectTransform canvas = CreateCanvas(scene, camera);
                foreach (DiagramKey key in KeyboardMouseDiagramLayout.Keys)
                {
                    AddKey(canvas, keySprite, key);
                }

                AddMouse(canvas, mouseSprite);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                diagram = ReadCoverageAsWhite(renderTexture);
                File.WriteAllBytes(diagramPath, diagram.EncodeToPNG());
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
                renderTexture.Release();
                Object.DestroyImmediate(renderTexture);
                Object.DestroyImmediate(diagram);
                Object.DestroyImmediate(keySprite);
                Object.DestroyImmediate(mouseSprite);
                Object.DestroyImmediate(keyRim);
                Object.DestroyImmediate(mouseRim);
            }

            ImportAsSprite(diagramPath);
        }

        #endregion

        #region Private Methods

        /// <returns>The blank's rim in white, cropped to its visible pixels.</returns>
        private static Texture2D LoadRim(string blankPath)
        {
            if (!File.Exists(blankPath))
            {
                throw new InvalidOperationException($"Blank '{blankPath}' for the keyboard and mouse diagram does not exist.");
            }

            var blank = new Texture2D(2, 2);
            blank.LoadImage(File.ReadAllBytes(blankPath));
            RectInt bounds = FindVisibleBounds(blank);
            var rim = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false);
            for (int y = 0; y < bounds.height; y++)
            {
                for (int x = 0; x < bounds.width; x++)
                {
                    Color pixel = blank.GetPixel(bounds.x + x, bounds.y + y);
                    float rimAlpha01 = Mathf.InverseLerp(RIM_GONE_ABOVE_LUMINANCE01, RIM_FULL_BELOW_LUMINANCE01, pixel.grayscale);
                    rim.SetPixel(x, y, new Color(1f, 1f, 1f, pixel.a * rimAlpha01));
                }
            }

            rim.Apply();
            Object.DestroyImmediate(blank);
            return rim;
        }

        private static RectInt FindVisibleBounds(Texture2D texture)
        {
            int minX = texture.width, minY = texture.height, maxX = -1, maxY = -1;
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    if (texture.GetPixel(x, y).a > 0f)
                    {
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }

            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        /// <remarks>The mouse's control hotspots were measured on this blank; a different shape would misplace them.</remarks>
        private static void ThrowIfMouseDoesNotFit(Texture2D mouseRim, string mouseBlankPath)
        {
            float aspect = (float)mouseRim.width / mouseRim.height;
            if (Mathf.Abs(aspect - KeyboardMouseDiagramLayout.MOUSE_ASPECT) > MOUSE_ASPECT_TOLERANCE)
            {
                throw new InvalidOperationException(
                    $"Mouse blank '{mouseBlankPath}' is {aspect:0.###} wide per unit of height, but the layout was measured on " +
                    $"{KeyboardMouseDiagramLayout.MOUSE_ASPECT:0.###}. Measure the mouse controls again.");
            }
        }

        private static Camera CreateCamera(Scene scene, RenderTexture renderTexture)
        {
            var cameraObject = new GameObject("Diagram Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.scene = scene;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.targetTexture = renderTexture;
            return camera;
        }

        private static RectTransform CreateCanvas(Scene scene, Camera camera)
        {
            var canvasObject = new GameObject("Diagram Canvas", typeof(RectTransform));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            return (RectTransform)canvasObject.transform;
        }

        private static void AddKey(RectTransform canvas, Sprite keySprite, DiagramKey key)
        {
            RectTransform keyRect = CreateChild(canvas, key.ControlPath, key.RectKeyUnits);
            var rim = keyRect.gameObject.AddComponent<Image>();
            rim.sprite = keySprite;
            rim.type = Image.Type.Sliced;
            rim.pixelsPerUnitMultiplier = keySprite.rect.height / (key.RectKeyUnits.height * KeyboardMouseDiagramLayout.PIXELS_PER_KEY_UNIT);

            RectTransform labelRect = CreateChild(keyRect, "Label", new Rect(0f, 0f, 1f, 1f), isRelativeToParent: true);
            var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset;
            label.text = key.Label;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.color = Color.white;
            label.fontSize = FitFontSizePixels(label, key);
            label.ForceMeshUpdate();
        }

        /// <remarks>
        /// Measured rather than auto-sized: the preview canvas sizes its rects only when it renders, after TMP has laid out.
        /// </remarks>
        private static float FitFontSizePixels(TextMeshProUGUI label, DiagramKey key)
        {
            float pixelsPerKeyUnit = KeyboardMouseDiagramLayout.PIXELS_PER_KEY_UNIT;
            float fontSizePixels = (key.Label.Length == 1 ? SINGLE_CHARACTER_FONT_SIZE_KEY_UNITS : WORD_FONT_SIZE_KEY_UNITS)
                * pixelsPerKeyUnit;
            label.fontSize = fontSizePixels;
            float labelWidthPixels = label.GetPreferredValues(key.Label).x;
            float roomPixels = (key.RectKeyUnits.width - 2f * LABEL_MARGIN_KEY_UNITS) * pixelsPerKeyUnit;
            return labelWidthPixels > roomPixels ? fontSizePixels * roomPixels / labelWidthPixels : fontSizePixels;
        }

        private static void AddMouse(RectTransform canvas, Sprite mouseSprite)
        {
            RectTransform mouseRect = CreateChild(canvas, "Mouse", KeyboardMouseDiagramLayout.MOUSE_KEY_UNITS);
            mouseRect.gameObject.AddComponent<Image>().sprite = mouseSprite;
        }

        /// <param name="rect">In key units with a top-left origin, or in fractions of the parent when relative.</param>
        private static RectTransform CreateChild(RectTransform parent, string name, Rect rect, bool isRelativeToParent = false)
        {
            var child = new GameObject(name, typeof(RectTransform));
            var childRect = (RectTransform)child.transform;
            childRect.SetParent(parent, false);
            Vector2 scale = isRelativeToParent ? Vector2.one : Vector2.one / KeyboardMouseDiagramLayout.SIZE_KEY_UNITS;
            childRect.anchorMin = new Vector2(rect.xMin * scale.x, 1f - rect.yMax * scale.y);
            childRect.anchorMax = new Vector2(rect.xMax * scale.x, 1f - rect.yMin * scale.y);
            childRect.offsetMin = Vector2.zero;
            childRect.offsetMax = Vector2.zero;
            return childRect;
        }

        /// <remarks>
        /// White art on black stores each pixel's coverage in its color; reading that back as alpha avoids the darkened
        /// edges that blending into a transparent target leaves.
        /// </remarks>
        private static Texture2D ReadCoverageAsWhite(RenderTexture renderTexture)
        {
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var image = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGBA32, false, true);
            image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            RenderTexture.active = previousActive;

            Color32[] pixels = image.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, pixels[i].r);
            }

            image.SetPixels32(pixels);
            image.Apply();
            return image;
        }

        /// <remarks>
        /// Without mipmaps, like the gamepad diagrams: Unity leaves a non-power-of-two texture with mipmaps uncompressed,
        /// which made this one several megabytes.
        /// </remarks>
        private static void ImportAsSprite(string diagramPath)
        {
            AssetDatabase.ImportAsset(diagramPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(diagramPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        #endregion
    }
}
