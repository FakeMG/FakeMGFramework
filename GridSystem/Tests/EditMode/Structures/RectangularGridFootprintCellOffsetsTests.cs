using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.GridSystem.Tests.EditMode
{
    public sealed class RectangularGridFootprintCellOffsetsTests
    {
        #region Public Methods

        [Test]
        public void Create_AsymmetricBoundsWithNegativeOffsets_ReturnsCanonicalOffsets()
        {
            BoundsInt cellBounds = new(new Vector3Int(-2, 0, -1), new Vector3Int(3, 1, 2));

            IReadOnlyList<Vector3Int> occupiedCellOffsets =
                RectangularGridFootprintCellOffsets.Create(cellBounds);

            CollectionAssert.AreEqual(
                new[]
                {
                    new Vector3Int(-2, 0, -1),
                    new Vector3Int(-2, 0, 0),
                    new Vector3Int(-1, 0, -1),
                    new Vector3Int(-1, 0, 0),
                    new Vector3Int(0, 0, -1),
                    new Vector3Int(0, 0, 0),
                },
                occupiedCellOffsets);
        }

        [Test]
        public void TryCreateBounds_CanonicalOffsets_RestoresOriginalBounds()
        {
            BoundsInt expectedCellBounds =
                new(new Vector3Int(-2, 0, -1), new Vector3Int(3, 1, 2));
            IReadOnlyList<Vector3Int> occupiedCellOffsets =
                RectangularGridFootprintCellOffsets.Create(expectedCellBounds);

            bool wasCreated = RectangularGridFootprintCellOffsets.TryCreateBounds(
                occupiedCellOffsets,
                out BoundsInt actualCellBounds,
                out string failureReason);

            Assert.IsTrue(wasCreated, failureReason);
            Assert.AreEqual(expectedCellBounds, actualCellBounds);
        }

        [Test]
        public void TryCreateBounds_MissingOffsets_ReturnsFalse()
        {
            bool wasCreated = RectangularGridFootprintCellOffsets.TryCreateBounds(
                new List<Vector3Int>(),
                out _,
                out string failureReason);

            Assert.IsFalse(wasCreated);
            StringAssert.Contains("missing", failureReason);
        }

        [Test]
        public void TryCreateBounds_NullOffsets_ReturnsFalse()
        {
            bool wasCreated = RectangularGridFootprintCellOffsets.TryCreateBounds(
                null,
                out _,
                out string failureReason);

            Assert.IsFalse(wasCreated);
            StringAssert.Contains("missing", failureReason);
        }

        [Test]
        public void TryCreateBounds_SingleCell_ReturnsSingleCellBounds()
        {
            Vector3Int cellOffset = new(-4, 3, 7);

            bool wasCreated = RectangularGridFootprintCellOffsets.TryCreateBounds(
                new[] { cellOffset },
                out BoundsInt cellBounds,
                out string failureReason);

            Assert.IsTrue(wasCreated, failureReason);
            Assert.AreEqual(new BoundsInt(cellOffset, Vector3Int.one), cellBounds);
        }

        [Test]
        public void TryCreateBounds_UnorderedThreeDimensionalOffsets_ReturnsFilledBounds()
        {
            List<Vector3Int> occupiedCellOffsets = new()
            {
                new(1, 1, 0),
                new(0, 0, 0),
                new(1, 0, 0),
                new(0, 1, 0),
            };

            bool wasCreated = RectangularGridFootprintCellOffsets.TryCreateBounds(
                occupiedCellOffsets,
                out BoundsInt cellBounds,
                out string failureReason);

            Assert.IsTrue(wasCreated, failureReason);
            Assert.AreEqual(new BoundsInt(Vector3Int.zero, new Vector3Int(2, 2, 1)), cellBounds);
        }

        [Test]
        public void TryCreateBounds_DuplicateOffset_ReturnsFalse()
        {
            List<Vector3Int> occupiedCellOffsets = new()
            {
                Vector3Int.zero,
                Vector3Int.zero,
            };

            bool wasCreated = RectangularGridFootprintCellOffsets.TryCreateBounds(
                occupiedCellOffsets,
                out _,
                out string failureReason);

            Assert.IsFalse(wasCreated);
            StringAssert.Contains("duplicated", failureReason);
        }

        [Test]
        public void TryCreateBounds_NonRectangularOffsets_ReturnsFalse()
        {
            List<Vector3Int> occupiedCellOffsets = new()
            {
                new(0, 0, 0),
                new(1, 0, 0),
                new(1, 0, 1),
            };

            bool wasCreated = RectangularGridFootprintCellOffsets.TryCreateBounds(
                occupiedCellOffsets,
                out _,
                out string failureReason);

            Assert.IsFalse(wasCreated);
            StringAssert.Contains("rectangular", failureReason);
        }

        [Test]
        public void TryCreateBounds_CellRangeExceedsIntegerSize_ReturnsFalse()
        {
            List<Vector3Int> occupiedCellOffsets = new()
            {
                new(int.MinValue, 0, 0),
                new(int.MaxValue, 0, 0),
            };

            bool wasCreated = RectangularGridFootprintCellOffsets.TryCreateBounds(
                occupiedCellOffsets,
                out _,
                out string failureReason);

            Assert.IsFalse(wasCreated);
            StringAssert.Contains("supported grid bounds", failureReason);
        }

        #endregion
    }
}
