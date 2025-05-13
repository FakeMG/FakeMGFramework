using NUnit.Framework;
using UnityEngine;

namespace FakeMG.FakeMGFramework.ExtensionMethods.Tests {
    public class LayerMaskExtensionsTests {
        [Test]
        public void ContainLayer_LayerIsInMask_ReturnsTrue() {
            // Arrange
            int layer = 5;
            LayerMask mask = 1 << layer;

            // Act
            bool result = mask.ContainLayer(layer);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ContainLayer_LayerIsNotInMask_ReturnsFalse() {
            // Arrange
            int layerInMask = 5;
            int layerNotInMask = 7;
            LayerMask mask = 1 << layerInMask;

            // Act
            bool result = mask.ContainLayer(layerNotInMask);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ContainLayer_EmptyMask_ReturnsFalse() {
            // Arrange
            int layer = 5;
            LayerMask emptyMask = 0;

            // Act
            bool result = emptyMask.ContainLayer(layer);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ContainLayer_MultipleLayers_ChecksCorrectly() {
            // Arrange
            int layer1 = 3;
            int layer2 = 5;
            int layer3 = 8;
            LayerMask mask = (1 << layer1) | (1 << layer3); // Contains layers 3 and 8

            // Act & Assert
            Assert.IsTrue(mask.ContainLayer(layer1), "Should contain layer 3");
            Assert.IsFalse(mask.ContainLayer(layer2), "Should not contain layer 5");
            Assert.IsTrue(mask.ContainLayer(layer3), "Should contain layer 8");
        }
        
        [Test]
        public void ContainLayerMasks_WhenMaskContainsAllLayersOfMaskB_ReturnsTrue() {
            // Arrange
            LayerMask mask = (1 << 3) | (1 << 5) | (1 << 7); // Layers 3, 5, and 7
            LayerMask maskB = (1 << 3) | (1 << 5);          // Layers 3 and 5

            // Act
            bool result = mask.ContainLayerMasks(maskB);

            // Assert
            Assert.IsTrue(result, "Mask should contain all layers from maskB");
        }

        [Test]
        public void ContainLayerMasks_WhenMaskMissingLayersFromMaskB_ReturnsFalse() {
            // Arrange
            LayerMask mask = (1 << 3) | (1 << 7);           // Layers 3 and 7
            LayerMask maskB = (1 << 3) | (1 << 5);          // Layers 3 and 5

            // Act
            bool result = mask.ContainLayerMasks(maskB);

            // Assert
            Assert.IsFalse(result, "Mask is missing layer 5 from maskB");
        }

        [Test]
        public void ContainLayerMasks_WithEmptyMaskB_ReturnsTrue() {
            // Arrange
            LayerMask mask = (1 << 3) | (1 << 5);           // Layers 3 and 5
            LayerMask emptyMask = 0;                        // No layers

            // Act
            bool result = mask.ContainLayerMasks(emptyMask);

            // Assert
            Assert.IsTrue(result, "Any mask should contain an empty mask");
        }

        [Test]
        public void ContainLayerMasks_WithEmptyMask_ReturnsFalseForNonEmptyMaskB() {
            // Arrange
            LayerMask emptyMask = 0;                        // No layers
            LayerMask maskB = (1 << 3);                     // Layer 3

            // Act
            bool result = emptyMask.ContainLayerMasks(maskB);

            // Assert
            Assert.IsFalse(result, "Empty mask cannot contain layers from non-empty maskB");
        }

        [Test]
        public void ContainLayerMasks_IdenticalMasks_ReturnsTrue() {
            // Arrange
            LayerMask mask = (1 << 3) | (1 << 5) | (1 << 7); // Layers 3, 5, and 7
            LayerMask identicalMask = (1 << 3) | (1 << 5) | (1 << 7); // Same layers

            // Act
            bool result = mask.ContainLayerMasks(identicalMask);

            // Assert
            Assert.IsTrue(result, "Mask should contain all layers from an identical mask");
        }

        [Test]
        public void AddLayerMasks_WithNoOverlap_CombinesMasks() {
            // Arrange
            LayerMask mask1 = 1 << 3; // Layer 3
            LayerMask mask2 = 1 << 5; // Layer 5
            LayerMask expected = (1 << 3) | (1 << 5); // Both layers

            // Act
            LayerMask result = mask1.AddLayerMasks(mask2);

            // Assert
            Assert.AreEqual(expected.value, result.value);
            Assert.IsTrue(result.ContainLayer(3));
            Assert.IsTrue(result.ContainLayer(5));
        }

        [Test]
        public void AddLayerMasks_WithOverlap_CombinesCorrectly() {
            // Arrange
            LayerMask mask1 = (1 << 3) | (1 << 5); // Layers 3 and 5
            LayerMask mask2 = (1 << 5) | (1 << 7); // Layers 5 and 7
            LayerMask expected = (1 << 3) | (1 << 5) | (1 << 7); // Layers 3, 5, and 7

            // Act
            LayerMask result = mask1.AddLayerMasks(mask2);

            // Assert
            Assert.AreEqual(expected.value, result.value);
            Assert.IsTrue(result.ContainLayer(3));
            Assert.IsTrue(result.ContainLayer(5));
            Assert.IsTrue(result.ContainLayer(7));
        }

        [Test]
        public void RemoveLayerMasks_WithPresentLayer_RemovesLayers() {
            // Arrange
            LayerMask original = (1 << 3) | (1 << 5) | (1 << 7); // Layers 3, 5, and 7
            LayerMask toRemove = (1 << 5); // Layer 5

            // Act
            LayerMask result = original.RemoveLayerMasks(toRemove);

            // Assert
            // Note: The current implementation uses XOR (^), so we expect:
            // Layers that are in both masks will be removed
            // Layers that are only in one mask will remain
            Assert.IsTrue(result.ContainLayer(3));
            Assert.IsFalse(result.ContainLayer(5));
            Assert.IsTrue(result.ContainLayer(7));
        }

        [Test]
        public void RemoveLayerMasks_WithNonPresentLayer_Unchanged() {
            // Arrange
            LayerMask original = (1 << 3) | (1 << 7); // Layers 3 and 7
            LayerMask toRemove = (1 << 5); // Layer 5 (not in original)

            // Act
            LayerMask result = original.RemoveLayerMasks(toRemove);

            // Assert
            Assert.IsTrue(result.ContainLayer(3));
            Assert.IsFalse(result.ContainLayer(5));
            Assert.IsTrue(result.ContainLayer(7));
        }
    }
}