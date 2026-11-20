using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.InputHints.Editor
{
    /// <summary>Where each shape sits on the masks sheet, one rect per shape in packing order.</summary>
    internal sealed class DiagramMaskSheetLayout
    {
        /// <summary>Each sheet pixel stands for a square of this many diagram pixels per side.</summary>
        public const int DOWNSCALE_FACTOR = 2;

        public IReadOnlyList<RectInt> SheetRects { get; }
        public Vector2Int SheetSizePixels { get; }

        public DiagramMaskSheetLayout(IReadOnlyList<RectInt> sheetRects, Vector2Int sheetSizePixels)
        {
            SheetRects = sheetRects;
            SheetSizePixels = sheetSizePixels;
        }

        #region Public Methods

        public static int ToDownscaledLength(int lengthPixels)
        {
            return (lengthPixels + DOWNSCALE_FACTOR - 1) / DOWNSCALE_FACTOR;
        }

        public Rect ToUvRect(int shapeIndex)
        {
            RectInt sheetRect = SheetRects[shapeIndex];
            return new Rect(
                (float)sheetRect.x / SheetSizePixels.x,
                (float)sheetRect.y / SheetSizePixels.y,
                (float)sheetRect.width / SheetSizePixels.x,
                (float)sheetRect.height / SheetSizePixels.y);
        }

        /// <returns>The diagram area the shape's sheet rect covers, which can be a pixel wider than the shape.</returns>
        public Vector2 GetSourceSizePixels(int shapeIndex)
        {
            RectInt sheetRect = SheetRects[shapeIndex];
            return new Vector2(sheetRect.width, sheetRect.height) * DOWNSCALE_FACTOR;
        }

        #endregion
    }
}
