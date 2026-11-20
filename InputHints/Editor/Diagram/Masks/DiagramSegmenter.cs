using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.InputHints.Editor
{
    /// <summary>Traces a line-art diagram into the shapes its masks fill, in memory.</summary>
    /// <remarks>
    /// A control's shape is the enclosed shape its hotspot centre sits in, outline and label included. A shape left open
    /// within the hotspot, like one arm of a d-pad, is traced through open space, grown under its outline and cut off at the
    /// hotspot.
    /// </remarks>
    internal static class DiagramSegmenter
    {
        private const float SEARCH_BOX_GROWTH01 = 0.4f;
        private const int SEED_SEARCH_RADIUS_PIXELS = 12;
        private const float MIN_WHOLE_CONTROL_SIZE01 = 0.5f;
        private const int LINE_COVER_PIXELS = 6;

        #region Public Methods

        /// <returns>Everything the outside cannot reach without crossing a line, so the outlines themselves are included.</returns>
        public static DiagramShape TraceFill(DiagramLineArt lineArt)
        {
            var whole = new RectInt(Vector2Int.zero, lineArt.SizePixels);
            bool[] isOutside = FloodFromBorder(lineArt, whole);
            var isInside = new bool[isOutside.Length];
            for (int i = 0; i < isOutside.Length; i++)
            {
                isInside[i] = !isOutside[i];
            }

            return DiagramShape.Copy(null, isInside, whole, lineArt.SizePixels.x);
        }

        /// <param name="diagramPath">Named in the error only.</param>
        /// <returns>One shape per hotspot, in the same order.</returns>
        /// <exception cref="InvalidOperationException">A hotspot's centre sits on no enclosed area of the diagram.</exception>
        public static List<DiagramShape> TraceControls(DiagramLineArt lineArt, IReadOnlyList<DiagramHotspotPixels> hotspots, string diagramPath)
        {
            var controls = new List<DiagramShape>(hotspots.Count);
            foreach (DiagramHotspotPixels hotspot in hotspots)
            {
                controls.Add(TraceControl(lineArt, hotspot, diagramPath));
            }

            return controls;
        }

        #endregion

        #region Private Methods

        private static bool[] FloodFromBorder(DiagramLineArt lineArt, RectInt box)
        {
            return PixelFlood.Flood(lineArt.SizePixels, box, PixelFlood.BorderOf(box), pixel => !lineArt.IsLine(pixel));
        }

        /// <remarks>
        /// Prefers the whole enclosed shape around the hotspot's centre, outline and label included, so a key fills its
        /// face and not just a letter's counter. A shape left open within the search box, like one arm of a d-pad, is
        /// traced through open space instead and cut off at the measured hotspot.
        /// </remarks>
        private static DiagramShape TraceControl(DiagramLineArt lineArt, DiagramHotspotPixels hotspot, string diagramPath)
        {
            RectInt searchBox = ToSearchBox(lineArt.SizePixels, hotspot);
            RectInt measuredBox = ToMeasuredBox(lineArt.SizePixels, hotspot);
            bool[] isOutside = FloodFromBorder(lineArt, searchBox);
            if (TryFindNear(searchBox, pixel => !isOutside[lineArt.IndexOf(pixel)], out Vector2Int enclosedSeed))
            {
                bool[] isEnclosed = FloodEnclosedArea(lineArt, searchBox, isOutside, enclosedSeed);
                if (IsWholeControl(isEnclosed, searchBox, measuredBox, lineArt.SizePixels.x))
                {
                    FillHoles(searchBox, isEnclosed, lineArt.SizePixels);
                    return DiagramShape.Crop(hotspot.ControlPath, isEnclosed, searchBox, lineArt.SizePixels.x);
                }
            }

            if (!TryFindNear(searchBox, pixel => !lineArt.IsLine(pixel), out Vector2Int openSeed))
            {
                throw new InvalidOperationException(
                    $"Hotspot '{hotspot.ControlPath}' of '{diagramPath}' has no area near its centre to highlight.");
            }

            bool[] isInShape = FloodOpenArea(lineArt, searchBox, openSeed);
            ClearOutside(isInShape, measuredBox, searchBox, lineArt.SizePixels.x);
            CoverOwnOutline(lineArt, measuredBox, isInShape);
            FillHoles(measuredBox, isInShape, lineArt.SizePixels);
            return DiagramShape.Crop(hotspot.ControlPath, isInShape, measuredBox, lineArt.SizePixels.x);
        }

        private static RectInt ToSearchBox(Vector2Int diagramSizePixels, DiagramHotspotPixels hotspot)
        {
            return ToPixelBox(diagramSizePixels, hotspot, 1f + SEARCH_BOX_GROWTH01);
        }

        private static RectInt ToMeasuredBox(Vector2Int diagramSizePixels, DiagramHotspotPixels hotspot)
        {
            return ToPixelBox(diagramSizePixels, hotspot, 1f);
        }

        /// <remarks>Hotspots are measured from the top-left like an image; pixels here count up from the bottom.</remarks>
        private static RectInt ToPixelBox(Vector2Int diagramSizePixels, DiagramHotspotPixels hotspot, float scale)
        {
            float halfWidthPixels = hotspot.WidthPixels * scale / 2f;
            float halfHeightPixels = hotspot.HeightPixels * scale / 2f;
            float centerYPixels = diagramSizePixels.y - hotspot.CenterYPixels;
            int xMin = Mathf.Max(0, Mathf.FloorToInt(hotspot.CenterXPixels - halfWidthPixels));
            int yMin = Mathf.Max(0, Mathf.FloorToInt(centerYPixels - halfHeightPixels));
            int xMax = Mathf.Min(diagramSizePixels.x, Mathf.CeilToInt(hotspot.CenterXPixels + halfWidthPixels));
            int yMax = Mathf.Min(diagramSizePixels.y, Mathf.CeilToInt(centerYPixels + halfHeightPixels));
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private static bool TryFindNear(RectInt searchBox, Func<Vector2Int, bool> isWanted, out Vector2Int found)
        {
            Vector2Int center = Vector2Int.RoundToInt(searchBox.center);
            for (int radiusPixels = 0; radiusPixels <= SEED_SEARCH_RADIUS_PIXELS; radiusPixels++)
            {
                for (int y = center.y - radiusPixels; y <= center.y + radiusPixels; y++)
                {
                    for (int x = center.x - radiusPixels; x <= center.x + radiusPixels; x++)
                    {
                        var candidate = new Vector2Int(x, y);
                        if (searchBox.Contains(candidate) && isWanted(candidate))
                        {
                            found = candidate;
                            return true;
                        }
                    }
                }
            }

            found = default;
            return false;
        }

        /// <remarks>Crosses lines too, so the shape takes in its own outline and any symbol drawn inside it.</remarks>
        private static bool[] FloodEnclosedArea(DiagramLineArt lineArt, RectInt box, bool[] isOutside, Vector2Int seed)
        {
            return PixelFlood.Flood(lineArt.SizePixels, box, new[] { seed }, pixel => !isOutside[lineArt.IndexOf(pixel)]);
        }

        /// <remarks>
        /// Rejects a shape that runs out of the search box, so it continues beyond the control, and one far smaller than the
        /// hotspot, like a symbol drawn loose inside an open area.
        /// </remarks>
        private static bool IsWholeControl(bool[] isInShape, RectInt searchBox, RectInt measuredBox, int diagramWidthPixels)
        {
            RectInt bounds = DiagramShape.FindCoveredBounds(isInShape, searchBox, diagramWidthPixels);
            bool touchesSearchBoxEdge = bounds.xMin == searchBox.xMin || bounds.yMin == searchBox.yMin
                || bounds.xMax == searchBox.xMax || bounds.yMax == searchBox.yMax;
            bool coversHotspot = bounds.width >= measuredBox.width * MIN_WHOLE_CONTROL_SIZE01
                && bounds.height >= measuredBox.height * MIN_WHOLE_CONTROL_SIZE01;
            return !touchesSearchBoxEdge && coversHotspot;
        }

        private static bool[] FloodOpenArea(DiagramLineArt lineArt, RectInt box, Vector2Int seed)
        {
            return PixelFlood.Flood(lineArt.SizePixels, box, new[] { seed }, pixel => !lineArt.IsLine(pixel));
        }

        private static void ClearOutside(bool[] isInShape, RectInt keptBox, RectInt searchBox, int diagramWidthPixels)
        {
            for (int y = searchBox.yMin; y < searchBox.yMax; y++)
            {
                for (int x = searchBox.xMin; x < searchBox.xMax; x++)
                {
                    if (!keptBox.Contains(new Vector2Int(x, y)))
                    {
                        isInShape[y * diagramWidthPixels + x] = false;
                    }
                }
            }
        }

        /// <remarks>Grows only into line pixels, so the shape slides under its outline without spilling into open space.</remarks>
        private static void CoverOwnOutline(DiagramLineArt lineArt, RectInt box, bool[] isInShape)
        {
            for (int step = 0; step < LINE_COVER_PIXELS; step++)
            {
                var grown = new List<int>();
                for (int y = box.yMin; y < box.yMax; y++)
                {
                    for (int x = box.xMin; x < box.xMax; x++)
                    {
                        var pixel = new Vector2Int(x, y);
                        if (!isInShape[lineArt.IndexOf(pixel)] && lineArt.IsLine(pixel) && TouchesShape(lineArt, box, isInShape, pixel))
                        {
                            grown.Add(lineArt.IndexOf(pixel));
                        }
                    }
                }

                foreach (int index in grown)
                {
                    isInShape[index] = true;
                }
            }
        }

        private static bool TouchesShape(DiagramLineArt lineArt, RectInt box, bool[] isInShape, Vector2Int pixel)
        {
            foreach (Vector2Int offset in PixelFlood.NEIGHBOUR_OFFSETS)
            {
                Vector2Int neighbour = pixel + offset;
                if (box.Contains(neighbour) && isInShape[lineArt.IndexOf(neighbour)])
                {
                    return true;
                }
            }

            return false;
        }

        /// <remarks>Fills what the shape encloses, such as the strokes and counters of a key's label.</remarks>
        private static void FillHoles(RectInt box, bool[] isInShape, Vector2Int diagramSizePixels)
        {
            bool[] isOutside = PixelFlood.Flood(
                diagramSizePixels,
                box,
                PixelFlood.BorderOf(box),
                pixel => !isInShape[pixel.y * diagramSizePixels.x + pixel.x]);
            for (int y = box.yMin; y < box.yMax; y++)
            {
                for (int x = box.xMin; x < box.xMax; x++)
                {
                    int index = y * diagramSizePixels.x + x;
                    if (!isOutside[index])
                    {
                        isInShape[index] = true;
                    }
                }
            }
        }

        #endregion
    }
}
