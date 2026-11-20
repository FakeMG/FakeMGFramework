using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FakeMG.InputHints.Editor
{
    /// <summary>
    /// Turns a line-art diagram into fill masks: one behind the whole device, and one per control, which the UI tints to
    /// highlight that control. The masks are saved as one texture beside the diagram.
    /// </summary>
    internal static class InputGlyphDiagramMaskBuilder
    {
        private const string MASKS_FILE_SUFFIX = "_Masks";

        #region Public Methods

        /// <param name="diagramPath">Project path of the diagram image.</param>
        /// <returns>The imported masks texture, and where the fill and each control sit on it.</returns>
        /// <exception cref="InvalidOperationException">A hotspot's centre sits on no enclosed area of the diagram.</exception>
        public static InputGlyphDiagramMasks BuildAndSaveMasks(string diagramPath, IReadOnlyList<DiagramHotspotPixels> hotspots)
        {
            DiagramLineArt lineArt = DiagramLineArt.Load(diagramPath);
            DiagramShape fill = DiagramSegmenter.TraceFill(lineArt);
            List<DiagramShape> controls = DiagramSegmenter.TraceControls(lineArt, hotspots, diagramPath);

            var shapes = new List<DiagramShape>(controls.Count + 1) { fill };
            shapes.AddRange(controls);
            DiagramMaskSheetLayout sheetLayout = DiagramMaskSheetPacker.Pack(shapes);
            string masksPath = Path.ChangeExtension(diagramPath, null) + MASKS_FILE_SUFFIX + ".png";
            Texture2D masks = DiagramMaskSheetWriter.WriteAndImport(masksPath, shapes, sheetLayout);

            var controlHotspots = new List<InputGlyphDiagramHotspot>(controls.Count);
            for (int shapeIndex = 1; shapeIndex < shapes.Count; shapeIndex++)
            {
                controlHotspots.Add(ToHotspot(shapes[shapeIndex], sheetLayout, shapeIndex, lineArt.SizePixels));
            }

            return new InputGlyphDiagramMasks(masks, sheetLayout.ToUvRect(0), controlHotspots);
        }

        #endregion

        #region Private Methods

        /// <remarks>Placed by the downscaled extent, which can be a pixel wider than the shape, so the mask lines up exactly.</remarks>
        private static InputGlyphDiagramHotspot ToHotspot(
            DiagramShape shape,
            DiagramMaskSheetLayout sheetLayout,
            int shapeIndex,
            Vector2Int diagramSizePixels)
        {
            Vector2 sizePixels = sheetLayout.GetSourceSizePixels(shapeIndex);
            Vector2 centerPixels = shape.Bounds.position + sizePixels / 2f;
            return new InputGlyphDiagramHotspot(
                shape.ControlPath,
                centerPixels / diagramSizePixels,
                sizePixels / diagramSizePixels,
                sheetLayout.ToUvRect(shapeIndex));
        }

        #endregion
    }

    internal readonly struct InputGlyphDiagramMasks
    {
        public Texture2D Masks { get; }
        public Rect FillUvRect { get; }
        public IReadOnlyList<InputGlyphDiagramHotspot> Hotspots { get; }

        public InputGlyphDiagramMasks(Texture2D masks, Rect fillUvRect, IReadOnlyList<InputGlyphDiagramHotspot> hotspots)
        {
            Masks = masks;
            FillUvRect = fillUvRect;
            Hotspots = hotspots;
        }
    }
}
