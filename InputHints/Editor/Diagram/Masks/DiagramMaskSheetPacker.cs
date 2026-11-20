using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.InputHints.Editor
{
    /// <summary>Places downscaled shapes on one sheet, in rows, in the order given.</summary>
    internal static class DiagramMaskSheetPacker
    {
        private const int MIN_SHEET_WIDTH_PIXELS = 256;
        private const int PACKING_GAP_PIXELS = 2;

        /// <remarks>Block compression needs both sides to be multiples of four; otherwise Unity stores the sheet uncompressed.</remarks>
        private const int COMPRESSION_BLOCK_PIXELS = 4;

        #region Public Methods

        /// <param name="shapes">The first sets the sheet's width, so it should be the widest.</param>
        public static DiagramMaskSheetLayout Pack(IReadOnlyList<DiagramShape> shapes)
        {
            int sheetWidthPixels = Mathf.Max(DiagramMaskSheetLayout.ToDownscaledLength(shapes[0].Bounds.width), MIN_SHEET_WIDTH_PIXELS);
            var sheetRects = new List<RectInt>(shapes.Count);
            int shelfX = 0, shelfY = 0, shelfHeight = 0;
            foreach (DiagramShape shape in shapes)
            {
                var sizePixels = new Vector2Int(
                    DiagramMaskSheetLayout.ToDownscaledLength(shape.Bounds.width),
                    DiagramMaskSheetLayout.ToDownscaledLength(shape.Bounds.height));
                if (shelfX > 0 && shelfX + sizePixels.x > sheetWidthPixels)
                {
                    shelfY += shelfHeight + PACKING_GAP_PIXELS;
                    shelfX = 0;
                    shelfHeight = 0;
                }

                sheetRects.Add(new RectInt(shelfX, shelfY, sizePixels.x, sizePixels.y));
                shelfX += sizePixels.x + PACKING_GAP_PIXELS;
                shelfHeight = Mathf.Max(shelfHeight, sizePixels.y);
            }

            var sheetSizePixels = new Vector2Int(RoundUpToBlock(sheetWidthPixels), RoundUpToBlock(shelfY + shelfHeight));
            return new DiagramMaskSheetLayout(sheetRects, sheetSizePixels);
        }

        #endregion

        #region Private Methods

        private static int RoundUpToBlock(int lengthPixels)
        {
            return (lengthPixels + COMPRESSION_BLOCK_PIXELS - 1) / COMPRESSION_BLOCK_PIXELS * COMPRESSION_BLOCK_PIXELS;
        }

        #endregion
    }
}
