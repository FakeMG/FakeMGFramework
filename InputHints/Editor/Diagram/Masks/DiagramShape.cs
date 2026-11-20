using UnityEngine;

namespace FakeMG.InputHints.Editor
{
    /// <summary>The pixels one mask covers, kept within its bounds on the diagram.</summary>
    internal sealed class DiagramShape
    {
        private readonly bool[] _isCovered;

        /// <summary>Null for the fill behind the whole diagram.</summary>
        public string ControlPath { get; }
        public RectInt Bounds { get; }

        private DiagramShape(string controlPath, bool[] isCovered, RectInt bounds)
        {
            ControlPath = controlPath;
            _isCovered = isCovered;
            Bounds = bounds;
        }

        #region Public Methods

        /// <remarks>Keeps the box whole, so the shape lines up with the box it was traced in.</remarks>
        public static DiagramShape Copy(string controlPath, bool[] diagramPixels, RectInt box, int diagramWidthPixels)
        {
            return new DiagramShape(controlPath, CopyBox(diagramPixels, box, diagramWidthPixels), box);
        }

        public static DiagramShape Crop(string controlPath, bool[] diagramPixels, RectInt box, int diagramWidthPixels)
        {
            RectInt bounds = FindCoveredBounds(diagramPixels, box, diagramWidthPixels);
            return new DiagramShape(controlPath, CopyBox(diagramPixels, bounds, diagramWidthPixels), bounds);
        }

        /// <returns>The smallest rect holding every set pixel inside the box.</returns>
        public static RectInt FindCoveredBounds(bool[] diagramPixels, RectInt box, int diagramWidthPixels)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = box.yMin; y < box.yMax; y++)
            {
                for (int x = box.xMin; x < box.xMax; x++)
                {
                    if (diagramPixels[y * diagramWidthPixels + x])
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

        /// <param name="blockX">Left of the block, relative to <see cref="Bounds"/>.</param>
        /// <param name="blockY">Bottom of the block, relative to <see cref="Bounds"/>.</param>
        /// <returns>The covered fraction of the block; pixels past the bounds count as uncovered.</returns>
        public float CoverageOfBlock(int blockX, int blockY, int blockSizePixels)
        {
            int coveredCount = 0;
            for (int y = blockY; y < blockY + blockSizePixels; y++)
            {
                for (int x = blockX; x < blockX + blockSizePixels; x++)
                {
                    if (x < Bounds.width && y < Bounds.height && _isCovered[y * Bounds.width + x])
                    {
                        coveredCount++;
                    }
                }
            }

            return (float)coveredCount / (blockSizePixels * blockSizePixels);
        }

        #endregion

        #region Private Methods

        private static bool[] CopyBox(bool[] diagramPixels, RectInt box, int diagramWidthPixels)
        {
            var isCovered = new bool[box.width * box.height];
            for (int y = 0; y < box.height; y++)
            {
                for (int x = 0; x < box.width; x++)
                {
                    isCovered[y * box.width + x] = diagramPixels[(box.y + y) * diagramWidthPixels + box.x + x];
                }
            }

            return isCovered;
        }

        #endregion
    }
}
