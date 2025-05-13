using UnityEngine;

namespace FakeMG.FakeMGFramework.ExtensionMethods {
    public static class LayerMaskExtensions {
        public static bool ContainLayer(this LayerMask mask, int layer) {
            return mask == (mask | (1 << layer));
        }
        
        public static bool ContainLayerMasks(this LayerMask mask, LayerMask maskB) {
            return (mask & maskB) == maskB; // Bitwise AND checks if all layers in maskB are in mask
        }

        public static LayerMask AddLayerMasks(this LayerMask mask, LayerMask maskB) {
            return mask | maskB; // Bitwise OR combines both LayerMasks
        }

        public static LayerMask RemoveLayerMasks(this LayerMask mask, LayerMask maskB) {
            return mask & ~maskB; // Bitwise AND with NOT properly removes layers
        }
    }
}