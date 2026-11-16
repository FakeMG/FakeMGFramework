using System.Collections.Generic;

namespace FakeMG.InputHints.Editor
{
    internal sealed class KenneyGlyphFamily
    {
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
