using System;
using UnityEngine;

namespace FakeMG.Framework.UI
{
    public readonly struct VirtualGridRange
    {
        public int FirstIndex { get; }
        public int EndIndexExclusive { get; }
        public int Count => EndIndexExclusive - FirstIndex;

        public VirtualGridRange(int firstIndex, int endIndexExclusive)
        {
            FirstIndex = firstIndex;
            EndIndexExclusive = endIndexExclusive;
        }
    }

    public sealed class VirtualGridLayout
    {
        private const int OVERSCAN_ROW_COUNT = 1;

        private readonly int _columnCount;
        private readonly Vector2 _cellSizePixels;
        private readonly Vector2 _spacingPixels;
        private readonly Vector2 _leadingPaddingPixels;
        private readonly float _bottomPaddingPixels;

        private float RowHeightPixels => _cellSizePixels.y + _spacingPixels.y;

        public VirtualGridLayout(int columnCount, Vector2 cellSizePixels, Vector2 spacingPixels,
            Vector2 leadingPaddingPixels, float bottomPaddingPixels)
        {
            if (columnCount <= 0 || cellSizePixels.x <= 0f || cellSizePixels.y <= 0f || spacingPixels.x < 0f || spacingPixels.y < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(cellSizePixels), "Grid columns and cell sizes must be positive; spacing cannot be negative.");
            }

            _columnCount = columnCount;
            _cellSizePixels = cellSizePixels;
            _spacingPixels = spacingPixels;
            _leadingPaddingPixels = leadingPaddingPixels;
            _bottomPaddingPixels = bottomPaddingPixels;
        }

        #region Public Methods

        public float GetContentHeightPixels(int itemCount)
        {
            int rowCount = GetRowCount(itemCount);
            return _leadingPaddingPixels.y + _bottomPaddingPixels + rowCount * _cellSizePixels.y +
                Mathf.Max(0, rowCount - 1) * _spacingPixels.y;
        }

        public int GetRequiredCardCount(int itemCount, float viewportHeightPixels)
        {
            int visibleRowCount = Mathf.CeilToInt(Mathf.Max(0f, viewportHeightPixels) / RowHeightPixels) +
                OVERSCAN_ROW_COUNT * 2 + 1;
            return Mathf.Min(Mathf.Max(0, itemCount), visibleRowCount * _columnCount);
        }

        public VirtualGridRange GetVisibleRange(int itemCount, float scrollOffsetPixels, float viewportHeightPixels)
        {
            if (itemCount <= 0)
            {
                return new VirtualGridRange(0, 0);
            }

            float maximumOffsetPixels = Mathf.Max(0f, GetContentHeightPixels(itemCount) - viewportHeightPixels);
            float clampedOffsetPixels = Mathf.Clamp(scrollOffsetPixels, 0f, maximumOffsetPixels);
            int firstRowIndex = Mathf.Max(0,
                Mathf.FloorToInt((clampedOffsetPixels - _leadingPaddingPixels.y) / RowHeightPixels) - OVERSCAN_ROW_COUNT);
            int endRowIndexExclusive = Mathf.Min(GetRowCount(itemCount),
                Mathf.CeilToInt((clampedOffsetPixels + viewportHeightPixels - _leadingPaddingPixels.y) / RowHeightPixels) +
                OVERSCAN_ROW_COUNT);
            return new VirtualGridRange(firstRowIndex * _columnCount, Mathf.Min(itemCount, endRowIndexExclusive * _columnCount));
        }

        public Vector2 GetItemPositionPixels(int itemIndex)
        {
            int columnIndex = itemIndex % _columnCount;
            int rowIndex = itemIndex / _columnCount;
            return new Vector2(_leadingPaddingPixels.x + columnIndex * (_cellSizePixels.x + _spacingPixels.x),
                -_leadingPaddingPixels.y - rowIndex * RowHeightPixels);
        }

        #endregion

        #region Private Methods

        private int GetRowCount(int itemCount)
        {
            return Mathf.CeilToInt(Mathf.Max(0, itemCount) / (float)_columnCount);
        }

        #endregion
    }
}
