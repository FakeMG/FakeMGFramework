using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FakeMG.InputHints.Editor
{
    /// <summary>Which pixels of a diagram are drawn lines. Pixels count from the bottom-left, row by row.</summary>
    internal sealed class DiagramLineArt
    {
        private const float LINE_MIN_ALPHA01 = 0.35f;

        private readonly bool[] _isLine;

        public Vector2Int SizePixels { get; }

        private DiagramLineArt(Vector2Int sizePixels, bool[] isLine)
        {
            SizePixels = sizePixels;
            _isLine = isLine;
        }

        #region Public Methods

        /// <param name="diagramPath">Read from disk as it is, whatever its import settings.</param>
        public static DiagramLineArt Load(string diagramPath)
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

            var lineArt = new DiagramLineArt(new Vector2Int(image.width, image.height), isLine);
            Object.DestroyImmediate(image);
            return lineArt;
        }

        public int IndexOf(Vector2Int pixel)
        {
            return pixel.y * SizePixels.x + pixel.x;
        }

        public bool IsLine(Vector2Int pixel)
        {
            return _isLine[IndexOf(pixel)];
        }

        #endregion
    }
}
