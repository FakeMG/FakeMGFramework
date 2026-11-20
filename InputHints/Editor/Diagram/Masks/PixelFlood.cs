using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.InputHints.Editor
{
    /// <summary>Flood fill over a pixel grid stored row by row.</summary>
    internal static class PixelFlood
    {
        public static readonly Vector2Int[] NEIGHBOUR_OFFSETS = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        #region Public Methods

        /// <summary>Spreads from the seeds to every pixel inside the box it can enter, through side-sharing neighbours.</summary>
        /// <param name="seeds">Seeds it cannot enter are skipped.</param>
        /// <returns>One flag per pixel of the grid, true where the flood reached.</returns>
        public static bool[] Flood(Vector2Int gridSizePixels, RectInt box, IEnumerable<Vector2Int> seeds, Func<Vector2Int, bool> canEnter)
        {
            var isReached = new bool[gridSizePixels.x * gridSizePixels.y];
            var frontier = new Queue<Vector2Int>();
            foreach (Vector2Int seed in seeds)
            {
                TryReach(seed);
            }

            while (frontier.Count > 0)
            {
                Vector2Int pixel = frontier.Dequeue();
                foreach (Vector2Int offset in NEIGHBOUR_OFFSETS)
                {
                    Vector2Int neighbour = pixel + offset;
                    if (box.Contains(neighbour))
                    {
                        TryReach(neighbour);
                    }
                }
            }

            return isReached;

            void TryReach(Vector2Int pixel)
            {
                int index = pixel.y * gridSizePixels.x + pixel.x;
                if (!isReached[index] && canEnter(pixel))
                {
                    isReached[index] = true;
                    frontier.Enqueue(pixel);
                }
            }
        }

        /// <returns>The box's outermost pixels; corners come twice.</returns>
        public static IEnumerable<Vector2Int> BorderOf(RectInt box)
        {
            for (int x = box.xMin; x < box.xMax; x++)
            {
                yield return new Vector2Int(x, box.yMin);
                yield return new Vector2Int(x, box.yMax - 1);
            }

            for (int y = box.yMin; y < box.yMax; y++)
            {
                yield return new Vector2Int(box.xMin, y);
                yield return new Vector2Int(box.xMax - 1, y);
            }
        }

        #endregion
    }
}
