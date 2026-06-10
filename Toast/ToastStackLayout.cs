using FakeMG.Framework;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Toast
{
    /// <summary>
    /// Pure stack layout math. Offsets assume each toast's pivot faces the stack direction
    /// (see <see cref="GetPivotForDirection"/>); the manager applies that pivot to pooled views.
    /// </summary>
    public static class ToastStackLayout
    {
        /// <summary>Pivot that makes a toast grow away from the entry position, so stacked toasts never overlap.</summary>
        public static Vector2 GetPivotForDirection(ToastStackDirection direction)
        {
            switch (direction)
            {
                case ToastStackDirection.Up: return new Vector2(0.5f, 0f);
                case ToastStackDirection.Down: return new Vector2(0.5f, 1f);
                case ToastStackDirection.Left: return new Vector2(1f, 0.5f);
                case ToastStackDirection.Right: return new Vector2(0f, 0.5f);
                default:
                    Echo.Error($"Unhandled stack direction '{direction}'. Falling back to Up pivot.");
                    return new Vector2(0.5f, 0f);
            }
        }

        /// <param name="sizesPixels">Visible sizes of active toasts, newest first.</param>
        /// <returns>Anchored position offsets relative to the entry position, same order. Index 0 is always zero.</returns>
        public static Vector2[] ComputeOffsets(
            IReadOnlyList<Vector2> sizesPixels,
            ToastStackDirection direction,
            float spacingPixels)
        {
            var offsets = new Vector2[sizesPixels.Count];
            float accumulatedDistancePixels = 0f;

            for (int i = 0; i < sizesPixels.Count; i++)
            {
                offsets[i] = ToOffset(accumulatedDistancePixels, direction);
                accumulatedDistancePixels += GetSizeAlongDirection(sizesPixels[i], direction) + spacingPixels;
            }

            return offsets;
        }

        private static Vector2 ToOffset(float distancePixels, ToastStackDirection direction)
        {
            switch (direction)
            {
                case ToastStackDirection.Up: return new Vector2(0f, distancePixels);
                case ToastStackDirection.Down: return new Vector2(0f, -distancePixels);
                case ToastStackDirection.Left: return new Vector2(-distancePixels, 0f);
                case ToastStackDirection.Right: return new Vector2(distancePixels, 0f);
                default:
                    Echo.Error($"Unhandled stack direction '{direction}'. Falling back to Up.");
                    return new Vector2(0f, distancePixels);
            }
        }

        private static float GetSizeAlongDirection(Vector2 sizePixels, ToastStackDirection direction)
        {
            bool isVertical = direction == ToastStackDirection.Up || direction == ToastStackDirection.Down;
            return isVertical ? sizePixels.y : sizePixels.x;
        }
    }
}
