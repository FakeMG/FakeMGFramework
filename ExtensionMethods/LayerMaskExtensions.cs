using UnityEngine;

namespace FakeMG.Framework.ExtensionMethods
{
    public static class LayerMaskExtensions
    {
        public static bool ContainLayer(this LayerMask mask, int layer)
        {
            return mask == (mask | (1 << layer));
        }

        public static bool ContainLayerMasks(this LayerMask mask, LayerMask maskB)
        {
            return (mask & maskB) == maskB;
        }

        public static LayerMask AddLayerMasks(this LayerMask mask, LayerMask maskB)
        {
            return mask | maskB;
        }

        public static LayerMask RemoveLayerMasks(this LayerMask mask, LayerMask maskB)
        {
            return mask & ~maskB;
        }
    }
}