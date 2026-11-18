using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FakeMG.InputHints.Editor
{
    /// <summary>
    /// Traces a line-art diagram into fill shapes: one behind the whole device, and one per control, which the UI tints to
    /// highlight that control. All shapes are packed into one texture saved beside the diagram.
    /// </summary>
    /// <remarks>
    /// A control's shape is the enclosed shape its hotspot centre sits in, outline and label included. A shape left open
    /// within the hotspot, like one arm of a d-pad, is traced through open space, grown under its outline and cut off at the
    /// hotspot.
    /// </remarks>
    internal static class InputGlyphDiagramMaskBuilder
    {
        private const string MASKS_FILE_SUFFIX = "_Masks";
        private const float LINE_MIN_ALPHA01 = 0.35f;
        private const float SEARCH_BOX_GROWTH01 = 0.4f;
        private const int SEED_SEARCH_RADIUS_PIXELS = 12;
        private const float MIN_WHOLE_CONTROL_SIZE01 = 0.5f;
        private const int LINE_COVER_PIXELS = 6;
        private const int DOWNSCALE_FACTOR = 2;
        private const int PACKING_GAP_PIXELS = 2;

        /// <remarks>Block compression needs both sides to be multiples of four; otherwise Unity stores the sheet uncompressed.</remarks>
        private const int COMPRESSION_BLOCK_PIXELS = 4;

        private static readonly Vector2Int[] NEIGHBOUR_OFFSETS = { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };

        #region Public Methods

        /// <param name="diagramPath">Project path of the diagram image.</param>
        /// <exception cref="InvalidOperationException">A hotspot's centre sits on no enclosed area of the diagram.</exception>
        public static InputGlyphDiagramMasks Build(string diagramPath, IReadOnlyList<DiagramHotspotPixels> hotspots)
        {
            LineArt lineArt = LoadLineArt(diagramPath);
            var shapes = new List<Shape> { TraceFill(lineArt) };
            foreach (DiagramHotspotPixels hotspot in hotspots)
            {
                shapes.Add(TraceControl(lineArt, hotspot, diagramPath));
            }

            List<RectInt> sheetRects = PackDownscaled(shapes, out Vector2Int sheetSizePixels);
            string masksPath = Path.ChangeExtension(diagramPath, null) + MASKS_FILE_SUFFIX + ".png";
            WriteSheet(masksPath, shapes, sheetRects, sheetSizePixels);
            var masks = AssetDatabase.LoadAssetAtPath<Texture2D>(masksPath);

            var controlHotspots = new List<InputGlyphDiagramHotspot>(hotspots.Count);
            for (int i = 1; i < shapes.Count; i++)
            {
                controlHotspots.Add(ToHotspot(shapes[i], sheetRects[i], sheetSizePixels, lineArt.SizePixels));
            }

            return new InputGlyphDiagramMasks(masks, ToUvRect(sheetRects[0], sheetSizePixels), controlHotspots);
        }

        #endregion

        #region Private Methods

        private static LineArt LoadLineArt(string diagramPath)
        {
            var image = new Texture2D(2, 2);
            image.LoadImage(File.ReadAllBytes(diagramPath));
            Color32[] pixels = image.GetPixels32();
            var isLine = new bool[pixels.Length];
            byte lineMinAlpha = (byte)(LINE_MIN_ALPHA01 * 255f);
            for (int i = 0; i < pixels.Length; i++)
            {
                isLine[i] = pixels[i].a >= lineMinAlpha;
            }

            var lineArt = new LineArt(new Vector2Int(image.width, image.height), isLine);
            Object.DestroyImmediate(image);
            return lineArt;
        }

        /// <remarks>Everything the outside cannot reach without crossing a line, so the outlines themselves are included.</remarks>
        private static Shape TraceFill(LineArt lineArt)
        {
            var whole = new RectInt(Vector2Int.zero, lineArt.SizePixels);
            bool[] isOutside = FloodFromBorder(lineArt, whole);
            var isInside = new bool[isOutside.Length];
            for (int i = 0; i < isOutside.Length; i++)
            {
                isInside[i] = !isOutside[i];
            }

            return Shape.Copy(null, isInside, whole, lineArt.SizePixels.x);
        }

        /// <remarks>
        /// Prefers the whole enclosed shape around the hotspot's centre, outline and label included, so a key fills its
        /// face and not just a letter's counter. A shape left open within the search box, like one arm of a d-pad, is
        /// traced through open space instead and cut off at the measured hotspot.
        /// </remarks>
        private static Shape TraceControl(LineArt lineArt, DiagramHotspotPixels hotspot, string diagramPath)
        {
            RectInt searchBox = ToSearchBox(lineArt.SizePixels, hotspot);
            RectInt measuredBox = ToMeasuredBox(lineArt.SizePixels, hotspot);
            bool[] isOutside = FloodFromBorder(lineArt, searchBox);
            if (TryFindNear(searchBox, pixel => !isOutside[lineArt.IndexOf(pixel)], out Vector2Int enclosedSeed))
            {
                bool[] isEnclosed = FloodEnclosedArea(lineArt, searchBox, isOutside, enclosedSeed);
                if (IsWholeControl(isEnclosed, searchBox, measuredBox, lineArt.SizePixels.x))
                {
                    FillHoles(searchBox, isEnclosed, lineArt.SizePixels.x);
                    return Shape.Crop(hotspot.ControlPath, isEnclosed, searchBox, lineArt.SizePixels.x);
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
            FillHoles(measuredBox, isInShape, lineArt.SizePixels.x);
            return Shape.Crop(hotspot.ControlPath, isInShape, measuredBox, lineArt.SizePixels.x);
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

        private static bool[] FloodFromBorder(LineArt lineArt, RectInt box)
        {
            var isReached = new bool[lineArt.SizePixels.x * lineArt.SizePixels.y];
            var frontier = new Queue<Vector2Int>();
            for (int x = box.xMin; x < box.xMax; x++)
            {
                Enqueue(new Vector2Int(x, box.yMin));
                Enqueue(new Vector2Int(x, box.yMax - 1));
            }

            for (int y = box.yMin; y < box.yMax; y++)
            {
                Enqueue(new Vector2Int(box.xMin, y));
                Enqueue(new Vector2Int(box.xMax - 1, y));
            }

            SpreadThroughOpenArea(lineArt, box, frontier, isReached);
            return isReached;

            void Enqueue(Vector2Int pixel)
            {
                int index = lineArt.IndexOf(pixel);
                if (!lineArt.IsLine(pixel) && !isReached[index])
                {
                    isReached[index] = true;
                    frontier.Enqueue(pixel);
                }
            }
        }

        /// <remarks>Crosses lines too, so the shape takes in its own outline and any symbol drawn inside it.</remarks>
        private static bool[] FloodEnclosedArea(LineArt lineArt, RectInt box, bool[] isOutside, Vector2Int seed)
        {
            var isReached = new bool[isOutside.Length];
            isReached[lineArt.IndexOf(seed)] = true;
            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(seed);
            while (frontier.Count > 0)
            {
                Vector2Int pixel = frontier.Dequeue();
                foreach (Vector2Int offset in NEIGHBOUR_OFFSETS)
                {
                    Vector2Int neighbour = pixel + offset;
                    if (!box.Contains(neighbour))
                    {
                        continue;
                    }

                    int index = lineArt.IndexOf(neighbour);
                    if (!isReached[index] && !isOutside[index])
                    {
                        isReached[index] = true;
                        frontier.Enqueue(neighbour);
                    }
                }
            }

            return isReached;
        }

        /// <remarks>
        /// Rejects a shape that runs out of the search box, so it continues beyond the control, and one far smaller than the
        /// hotspot, like a symbol drawn loose inside an open area.
        /// </remarks>
        private static bool IsWholeControl(bool[] isInShape, RectInt searchBox, RectInt measuredBox, int diagramWidthPixels)
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
            for (int y = searchBox.yMin; y < searchBox.yMax; y++)
            {
                for (int x = searchBox.xMin; x < searchBox.xMax; x++)
                {
                    if (isInShape[y * diagramWidthPixels + x])
                    {
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }

            bool touchesSearchBoxEdge = minX == searchBox.xMin || minY == searchBox.yMin
                || maxX == searchBox.xMax - 1 || maxY == searchBox.yMax - 1;
            bool coversHotspot = maxX - minX + 1 >= measuredBox.width * MIN_WHOLE_CONTROL_SIZE01
                && maxY - minY + 1 >= measuredBox.height * MIN_WHOLE_CONTROL_SIZE01;
            return !touchesSearchBoxEdge && coversHotspot;
        }

        private static bool[] FloodOpenArea(LineArt lineArt, RectInt box, Vector2Int seed)
        {
            var isReached = new bool[lineArt.SizePixels.x * lineArt.SizePixels.y];
            isReached[lineArt.IndexOf(seed)] = true;
            var frontier = new Queue<Vector2Int>();
            frontier.Enqueue(seed);
            SpreadThroughOpenArea(lineArt, box, frontier, isReached);
            return isReached;
        }

        private static void SpreadThroughOpenArea(LineArt lineArt, RectInt box, Queue<Vector2Int> frontier, bool[] isReached)
        {
            while (frontier.Count > 0)
            {
                Vector2Int pixel = frontier.Dequeue();
                foreach (Vector2Int offset in NEIGHBOUR_OFFSETS)
                {
                    Vector2Int neighbour = pixel + offset;
                    if (!box.Contains(neighbour))
                    {
                        continue;
                    }

                    int index = lineArt.IndexOf(neighbour);
                    if (!isReached[index] && !lineArt.IsLine(neighbour))
                    {
                        isReached[index] = true;
                        frontier.Enqueue(neighbour);
                    }
                }
            }
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
        private static void CoverOwnOutline(LineArt lineArt, RectInt box, bool[] isInShape)
        {
            for (int step = 0; step < LINE_COVER_PIXELS; step++)
            {
                var grown = new List<int>();
                for (int y = box.yMin; y < box.yMax; y++)
                {
                    for (int x = box.xMin; x < box.xMax; x++)
                    {
                        var pixel = new Vector2Int(x, y);
                        if (isInShape[lineArt.IndexOf(pixel)] || !lineArt.IsLine(pixel))
                        {
                            continue;
                        }

                        foreach (Vector2Int offset in NEIGHBOUR_OFFSETS)
                        {
                            Vector2Int neighbour = pixel + offset;
                            if (box.Contains(neighbour) && isInShape[lineArt.IndexOf(neighbour)])
                            {
                                grown.Add(lineArt.IndexOf(pixel));
                                break;
                            }
                        }
                    }
                }

                foreach (int index in grown)
                {
                    isInShape[index] = true;
                }
            }
        }

        /// <remarks>Fills what the shape encloses, such as the strokes and counters of a key's label.</remarks>
        private static void FillHoles(RectInt box, bool[] isInShape, int diagramWidthPixels)
        {
            int widthPixels = box.width;
            var isOutside = new bool[box.width * box.height];
            var frontier = new Queue<Vector2Int>();
            for (int y = 0; y < box.height; y++)
            {
                for (int x = 0; x < box.width; x++)
                {
                    bool isBorder = x == 0 || y == 0 || x == box.width - 1 || y == box.height - 1;
                    if (isBorder && !IsInShape(x, y))
                    {
                        isOutside[y * widthPixels + x] = true;
                        frontier.Enqueue(new Vector2Int(x, y));
                    }
                }
            }

            while (frontier.Count > 0)
            {
                Vector2Int pixel = frontier.Dequeue();
                foreach (Vector2Int offset in NEIGHBOUR_OFFSETS)
                {
                    Vector2Int neighbour = pixel + offset;
                    if (neighbour.x < 0 || neighbour.y < 0 || neighbour.x >= box.width || neighbour.y >= box.height)
                    {
                        continue;
                    }

                    int index = neighbour.y * widthPixels + neighbour.x;
                    if (!isOutside[index] && !IsInShape(neighbour.x, neighbour.y))
                    {
                        isOutside[index] = true;
                        frontier.Enqueue(neighbour);
                    }
                }
            }

            for (int y = 0; y < box.height; y++)
            {
                for (int x = 0; x < box.width; x++)
                {
                    if (!isOutside[y * widthPixels + x])
                    {
                        isInShape[(box.y + y) * diagramWidthPixels + box.x + x] = true;
                    }
                }
            }

            bool IsInShape(int x, int y)
            {
                return isInShape[(box.y + y) * diagramWidthPixels + box.x + x];
            }
        }

        private static List<RectInt> PackDownscaled(List<Shape> shapes, out Vector2Int sheetSizePixels)
        {
            int sheetWidthPixels = Mathf.Max(DownscaledLength(shapes[0].Bounds.width), 256);
            var sheetRects = new List<RectInt>(shapes.Count);
            int shelfX = 0, shelfY = 0, shelfHeight = 0;
            foreach (Shape shape in shapes)
            {
                var sizePixels = new Vector2Int(DownscaledLength(shape.Bounds.width), DownscaledLength(shape.Bounds.height));
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

            sheetSizePixels = new Vector2Int(RoundUpToBlock(sheetWidthPixels), RoundUpToBlock(shelfY + shelfHeight));
            return sheetRects;
        }

        private static int RoundUpToBlock(int lengthPixels)
        {
            return (lengthPixels + COMPRESSION_BLOCK_PIXELS - 1) / COMPRESSION_BLOCK_PIXELS * COMPRESSION_BLOCK_PIXELS;
        }

        private static int DownscaledLength(int lengthPixels)
        {
            return (lengthPixels + DOWNSCALE_FACTOR - 1) / DOWNSCALE_FACTOR;
        }

        /// <remarks>Averaging each block of pixels softens the shapes' edges as it shrinks them.</remarks>
        private static void WriteSheet(string masksPath, List<Shape> shapes, List<RectInt> sheetRects, Vector2Int sheetSizePixels)
        {
            var pixels = new Color32[sheetSizePixels.x * sheetSizePixels.y];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 0);
            }

            for (int shapeIndex = 0; shapeIndex < shapes.Count; shapeIndex++)
            {
                Shape shape = shapes[shapeIndex];
                RectInt sheetRect = sheetRects[shapeIndex];
                for (int y = 0; y < sheetRect.height; y++)
                {
                    for (int x = 0; x < sheetRect.width; x++)
                    {
                        float coverage01 = shape.CoverageOfBlock(x * DOWNSCALE_FACTOR, y * DOWNSCALE_FACTOR, DOWNSCALE_FACTOR);
                        pixels[(sheetRect.y + y) * sheetSizePixels.x + sheetRect.x + x].a = (byte)Mathf.RoundToInt(coverage01 * 255f);
                    }
                }
            }

            var sheet = new Texture2D(sheetSizePixels.x, sheetSizePixels.y, TextureFormat.RGBA32, false);
            sheet.SetPixels32(pixels);
            File.WriteAllBytes(masksPath, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            ImportAsMask(masksPath);
        }

        private static void ImportAsMask(string masksPath)
        {
            AssetDatabase.ImportAsset(masksPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(masksPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }

        /// <remarks>Placed by the downscaled extent, which can be a pixel wider than the shape, so the mask lines up exactly.</remarks>
        private static InputGlyphDiagramHotspot ToHotspot(Shape shape, RectInt sheetRect, Vector2Int sheetSizePixels, Vector2Int diagramSizePixels)
        {
            Vector2 sizePixels = new Vector2(sheetRect.width, sheetRect.height) * DOWNSCALE_FACTOR;
            Vector2 centerPixels = shape.Bounds.position + sizePixels / 2f;
            return new InputGlyphDiagramHotspot(
                shape.ControlPath,
                centerPixels / diagramSizePixels,
                sizePixels / diagramSizePixels,
                ToUvRect(sheetRect, sheetSizePixels));
        }

        private static Rect ToUvRect(RectInt sheetRect, Vector2Int sheetSizePixels)
        {
            return new Rect(
                (float)sheetRect.x / sheetSizePixels.x,
                (float)sheetRect.y / sheetSizePixels.y,
                (float)sheetRect.width / sheetSizePixels.x,
                (float)sheetRect.height / sheetSizePixels.y);
        }

        #endregion

        private readonly struct LineArt
        {
            private readonly bool[] _isLine;

            public Vector2Int SizePixels { get; }

            public LineArt(Vector2Int sizePixels, bool[] isLine)
            {
                SizePixels = sizePixels;
                _isLine = isLine;
            }

            public int IndexOf(Vector2Int pixel)
            {
                return pixel.y * SizePixels.x + pixel.x;
            }

            public bool IsLine(Vector2Int pixel)
            {
                return _isLine[IndexOf(pixel)];
            }
        }

        private sealed class Shape
        {
            private readonly bool[] _isCovered;

            /// <summary>Null for the fill behind the whole diagram.</summary>
            public string ControlPath { get; }
            public RectInt Bounds { get; }

            private Shape(string controlPath, bool[] isCovered, RectInt bounds)
            {
                ControlPath = controlPath;
                _isCovered = isCovered;
                Bounds = bounds;
            }

            /// <remarks>Keeps the box whole, so the shape lines up with the box it was traced in.</remarks>
            public static Shape Copy(string controlPath, bool[] diagramPixels, RectInt box, int diagramWidthPixels)
            {
                return new Shape(controlPath, CopyBox(diagramPixels, box, diagramWidthPixels), box);
            }

            public static Shape Crop(string controlPath, bool[] diagramPixels, RectInt searchBox, int diagramWidthPixels)
            {
                int minX = int.MaxValue, minY = int.MaxValue, maxX = -1, maxY = -1;
                for (int y = searchBox.yMin; y < searchBox.yMax; y++)
                {
                    for (int x = searchBox.xMin; x < searchBox.xMax; x++)
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

                var bounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
                return new Shape(controlPath, CopyBox(diagramPixels, bounds, diagramWidthPixels), bounds);
            }

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
        }
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
