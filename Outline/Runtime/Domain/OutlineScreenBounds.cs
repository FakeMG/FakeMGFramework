using UnityEngine;

namespace FakeMG.Outline
{
    public static class OutlineScreenBounds
    {
        #region Public Methods

        public static RectInt CalculatePixelRect(Bounds boundsMeters, Matrix4x4 worldToClip,
            int widthPixels, int heightPixels, int paddingPixels, bool doesUvStartAtTop, int allocationAlignmentPixels = 1)
        {
            if (widthPixels <= 0 || heightPixels <= 0)
            {
                return default;
            }

            Vector3 minimumMeters = boundsMeters.min;
            Vector3 maximumMeters = boundsMeters.max;
            Vector2 minimumNdc = new(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 maximumNdc = new(float.NegativeInfinity, float.NegativeInfinity);
            for (int cornerIndex = 0; cornerIndex < 8; cornerIndex++)
            {
                Vector4 cornerMeters = new(
                    (cornerIndex & 1) == 0 ? minimumMeters.x : maximumMeters.x,
                    (cornerIndex & 2) == 0 ? minimumMeters.y : maximumMeters.y,
                    (cornerIndex & 4) == 0 ? minimumMeters.z : maximumMeters.z, 1f);
                Vector4 cornerClip = worldToClip * cornerMeters;
                // A box intersecting the camera/near plane cannot safely be bounded by projected corners.
                if (cornerClip.w <= 0.0001f || Mathf.Abs(cornerClip.z) >= cornerClip.w)
                {
                    return new RectInt(0, 0, widthPixels, heightPixels);
                }

                Vector2 cornerNdc = new(cornerClip.x / cornerClip.w, cornerClip.y / cornerClip.w);
                if (doesUvStartAtTop)
                {
                    cornerNdc.y = -cornerNdc.y;
                }

                minimumNdc = Vector2.Min(minimumNdc, cornerNdc);
                maximumNdc = Vector2.Max(maximumNdc, cornerNdc);
            }

            int safePaddingPixels = Mathf.Max(0, paddingPixels);
            int minimumXPixels = Mathf.Clamp(Mathf.FloorToInt((minimumNdc.x * 0.5f + 0.5f) * widthPixels) - safePaddingPixels, 0, widthPixels);
            int minimumYPixels = Mathf.Clamp(Mathf.FloorToInt((minimumNdc.y * 0.5f + 0.5f) * heightPixels) - safePaddingPixels, 0, heightPixels);
            int maximumXPixels = Mathf.Clamp(Mathf.CeilToInt((maximumNdc.x * 0.5f + 0.5f) * widthPixels) + safePaddingPixels, 0, widthPixels);
            int maximumYPixels = Mathf.Clamp(Mathf.CeilToInt((maximumNdc.y * 0.5f + 0.5f) * heightPixels) + safePaddingPixels, 0, heightPixels);
            int rectangleWidthPixels = maximumXPixels - minimumXPixels;
            int rectangleHeightPixels = maximumYPixels - minimumYPixels;
            if (rectangleWidthPixels == 0 || rectangleHeightPixels == 0)
            {
                return default;
            }

            int safeAlignmentPixels = Mathf.Max(1, allocationAlignmentPixels);
            rectangleWidthPixels = Mathf.Min(widthPixels, Mathf.CeilToInt((float)rectangleWidthPixels / safeAlignmentPixels) * safeAlignmentPixels);
            rectangleHeightPixels = Mathf.Min(heightPixels, Mathf.CeilToInt((float)rectangleHeightPixels / safeAlignmentPixels) * safeAlignmentPixels);
            return new RectInt(Mathf.Min(minimumXPixels, widthPixels - rectangleWidthPixels),
                Mathf.Min(minimumYPixels, heightPixels - rectangleHeightPixels), rectangleWidthPixels, rectangleHeightPixels);
        }

        public static Vector4 CalculateClipScaleOffset(RectInt rectanglePixels, int widthPixels, int heightPixels, bool doesUvStartAtTop)
        {
            float scaleX01 = (float)rectanglePixels.width / widthPixels;
            float scaleY01 = (float)rectanglePixels.height / heightPixels;
            float offsetXNdc = (2f * rectanglePixels.x + rectanglePixels.width) / widthPixels - 1f;
            float offsetYNdc = (2f * rectanglePixels.y + rectanglePixels.height) / heightPixels - 1f;
            return new Vector4(scaleX01, scaleY01, offsetXNdc, doesUvStartAtTop ? -offsetYNdc : offsetYNdc);
        }

        #endregion
    }
}
