using System;
using System.Collections.Generic;
using FakeMG.Framework;

namespace FakeMG.Outline
{
    public sealed class RenderingLayerOutlineSelectionRegistry
    {
        private const int RENDERING_LAYER_BIT_COUNT = 32;

        private readonly Dictionary<object, uint> _masksByOwner = new();
        private readonly int[] _selectionCountsByBit = new int[RENDERING_LAYER_BIT_COUNT];
        private uint _activeRenderingLayerMask;

        public static RenderingLayerOutlineSelectionRegistry Shared { get; } = new();
        public int ActiveSelectionCount => _masksByOwner.Count;

        #region Public Methods

        public void RegisterSelection(object owner, uint renderingLayerMask)
        {
            if (owner == null)
            {
                Echo.Error("Outline selection registration was rejected because its owner is null.");
                return;
            }

            if (renderingLayerMask == 0)
            {
                UnregisterSelection(owner);
                return;
            }

            if (_masksByOwner.TryGetValue(owner, out uint previousMask))
            {
                if (previousMask == renderingLayerMask)
                {
                    return;
                }

                RemoveMaskCounts(previousMask);
            }

            _masksByOwner[owner] = renderingLayerMask;
            AddMaskCounts(renderingLayerMask);
        }

        public void UnregisterSelection(object owner)
        {
            if (owner == null)
            {
                Echo.Error("Outline selection cleanup was rejected because its owner is null.");
                return;
            }

            if (_masksByOwner.Remove(owner, out uint renderingLayerMask))
            {
                RemoveMaskCounts(renderingLayerMask);
            }
        }

        public bool HasSelection(uint renderingLayerMask)
        {
            return (_activeRenderingLayerMask & renderingLayerMask) != 0;
        }

        public void ClearSelections()
        {
            _masksByOwner.Clear();
            Array.Clear(_selectionCountsByBit, 0, _selectionCountsByBit.Length);
            _activeRenderingLayerMask = 0;
        }

        #endregion

        #region Private Methods

        private void RemoveMaskCounts(uint renderingLayerMask)
        {
            for (int bitIndex = 0; bitIndex < RENDERING_LAYER_BIT_COUNT; bitIndex++)
            {
                uint bitMask = 1u << bitIndex;
                if ((renderingLayerMask & bitMask) == 0)
                {
                    continue;
                }

                _selectionCountsByBit[bitIndex]--;
                if (_selectionCountsByBit[bitIndex] == 0)
                {
                    _activeRenderingLayerMask &= ~bitMask;
                }
            }
        }

        private void AddMaskCounts(uint renderingLayerMask)
        {
            for (int bitIndex = 0; bitIndex < RENDERING_LAYER_BIT_COUNT; bitIndex++)
            {
                uint bitMask = 1u << bitIndex;
                if ((renderingLayerMask & bitMask) != 0)
                {
                    _selectionCountsByBit[bitIndex]++;
                    _activeRenderingLayerMask |= bitMask;
                }
            }
        }

        #endregion
    }
}
