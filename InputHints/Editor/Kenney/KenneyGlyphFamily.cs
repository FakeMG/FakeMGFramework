using System.Collections.Generic;

namespace FakeMG.InputHints.Editor
{
    /// <summary>One device family of the Kenney Input Prompts pack: where its glyphs are and which glyph draws each control.</summary>
    internal sealed class KenneyGlyphFamily
    {
        /// <summary>Name of the pack's folder for this family, e.g. "Xbox Series".</summary>
        public string TextureFolderName { get; }
        public string GlyphSetAssetName { get; }
        public string[] DeviceLayouts { get; }

        /// <summary>Glyph file names without extension, keyed by control path relative to the device.</summary>
        public IReadOnlyDictionary<string, string> GlyphFileNamesByControlPath { get; }

        public KenneyGlyphFamily(
            string textureFolderName,
            string glyphSetAssetName,
            string[] deviceLayouts,
            IReadOnlyDictionary<string, string> iconNamesByControlPath)
        {
            TextureFolderName = textureFolderName;
            GlyphSetAssetName = glyphSetAssetName;
            DeviceLayouts = deviceLayouts;
            GlyphFileNamesByControlPath = iconNamesByControlPath;
        }
    }
}
