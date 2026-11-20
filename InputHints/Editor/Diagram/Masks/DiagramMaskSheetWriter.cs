using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FakeMG.InputHints.Editor
{
    /// <summary>Draws the packed shapes into a PNG and imports it as a mask sprite.</summary>
    internal static class DiagramMaskSheetWriter
    {
        #region Public Methods

        /// <remarks>Averaging each block of pixels softens the shapes' edges as it shrinks them.</remarks>
        /// <returns>The imported sheet.</returns>
        public static Texture2D WriteAndImport(string masksPath, IReadOnlyList<DiagramShape> shapes, DiagramMaskSheetLayout sheetLayout)
        {
            Vector2Int sheetSizePixels = sheetLayout.SheetSizePixels;
            var pixels = new Color32[sheetSizePixels.x * sheetSizePixels.y];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 0);
            }

            const int DOWNSCALE_FACTOR = DiagramMaskSheetLayout.DOWNSCALE_FACTOR;
            for (int shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
            {
                DiagramShape shape = shapes[shapeIndex];
                RectInt sheetRect = sheetLayout.SheetRects[shapeIndex];
                for (int y = 0; y < sheetRect.height; y++)
                {
                    for (int x = 0; x < sheetRect.width; x++)
                    {
                        float coverage01 = shape.CoverageOfBlock(x * DOWNSCALE_FACTOR, y * DOWNSCALE_FACTOR, DOWNSCALE_FACTOR);
                        pixels[(sheetRect.y + y) * sheetSizePixels.x + sheetRect.x + x].a = (byte)Mathf.RoundToInt(coverage01 * 255f);
                    }
                }
            }

            var sheet = new Texture2D(sheetSizePixels.x, sheetSizePixels.y, TextureFormat.RGBA32, false);
            sheet.SetPixels32(pixels);
            File.WriteAllBytes(masksPath, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            ImportAsMask(masksPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(masksPath);
        }

        #endregion

        #region Private Methods

        private static void ImportAsMask(string masksPath)
        {
            AssetDatabase.ImportAsset(masksPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(masksPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        #endregion
    }
}
