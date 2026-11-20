using System;
using System.Collections.Generic;

namespace FakeMG.InputHints.Editor
{
    internal sealed class InputGlyphFamily
    {
        public string TextureFolderName { get; }
        public string GlyphSetAssetName { get; }
        public string[] DeviceLayouts { get; }

        /// <summary>
        /// Relative to the pack's textures folder, with its extension, so a family can borrow another family's diagram.
        /// Null for none.
        /// </summary>
        public string DiagramFilePath { get; }
        public bool HasDiagram => DiagramFilePath != null;

        public IReadOnlyList<DiagramHotspotPixels> DiagramHotspots { get; }

        /// <summary>Keyed by control path relative to the device. File names have no extension.</summary>
        public IReadOnlyDictionary<string, string> GlyphFileNamesByControlPath { get; }

        public InputGlyphFamily(
            string textureFolderName,
            string glyphSetAssetName,
            string[] deviceLayouts,
            string diagramFilePath,
            IReadOnlyDictionary<string, string> glyphFileNamesByControlPath,
            IReadOnlyList<DiagramHotspotPixels> diagramHotspots = null)
        {
            TextureFolderName = textureFolderName;
            GlyphSetAssetName = glyphSetAssetName;
            DeviceLayouts = deviceLayouts;
            DiagramFilePath = diagramFilePath;
            GlyphFileNamesByControlPath = glyphFileNamesByControlPath;
            DiagramHotspots = diagramHotspots ?? Array.Empty<DiagramHotspotPixels>();
        }
    }
}
