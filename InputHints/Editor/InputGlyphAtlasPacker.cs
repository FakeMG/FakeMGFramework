using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace FakeMG.InputHints.Editor
{
    public static class InputGlyphAtlasPacker
    {
        private const int PADDING_PIXELS = 4;

        #region Public Methods

        /// <param name="atlasPath">A <c>.spriteatlasv2</c> path; created when missing.</param>
        public static void Pack(string atlasPath, IEnumerable<Sprite> sprites)
        {
            bool isNewAtlas = !AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath);
            SpriteAtlasAsset atlasAsset = isNewAtlas ? new SpriteAtlasAsset() : SpriteAtlasAsset.Load(atlasPath);
            if (!isNewAtlas)
            {
                atlasAsset.Remove(AssetDatabase.LoadAssetAtPath<SpriteAtlas>(atlasPath).GetPackables());
            }

            atlasAsset.Add(sprites.Distinct().Cast<Object>().ToArray());
            SpriteAtlasAsset.Save(atlasAsset, atlasPath);
            AssetDatabase.ImportAsset(atlasPath);
            ApplyGlyphSettings(atlasPath);
        }

        #endregion

        #region Private Methods

        /// <remarks>Glyphs are UI sprites: no mipmaps, and no rotation or tight packing, which UI Images do not support well.</remarks>
        private static void ApplyGlyphSettings(string atlasPath)
        {
            var importer = (SpriteAtlasImporter)AssetImporter.GetAtPath(atlasPath);
            importer.includeInBuild = true;
            importer.packingSettings = new SpriteAtlasPackingSettings
            {
                blockOffset = 1,
                padding = PADDING_PIXELS,
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = true,
            };
            importer.textureSettings = new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear,
            };
            importer.SaveAndReimport();
        }

        #endregion
    }
}
