using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.GridSystem.Tests.EditMode
{
    public sealed class GridFootprintTests
    {
        private const float CELL_SIZE_METERS = 1f;
        private const string GRID_FOOTPRINT_PREFAB_GUID = "6ad3613ca4f4f654284043be56fa7039";

        private GridFootprint _structureFootprint;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(GRID_FOOTPRINT_PREFAB_GUID));
            Assert.IsNotNull(prefab, $"Missing framework footprint fixture with GUID {GRID_FOOTPRINT_PREFAB_GUID}.");
            _structureFootprint = Object.Instantiate(prefab).GetComponentInChildren<GridFootprint>(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_structureFootprint.transform.root.gameObject);
        }

        [Test]
        public void GetOccupiedCells_AsymmetricBoundsAtZeroDegrees_ReturnsExpectedCells()
        {
            OverrideAsymmetricBounds();

            IReadOnlyList<Vector3Int> occupiedCells =
                _structureFootprint.GetOccupiedCells(Vector3Int.zero, 0, CELL_SIZE_METERS);

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
                occupiedCells);
        }

        [Test]
        public void GetOccupiedCells_AsymmetricBoundsAtNinetyDegrees_ReturnsExpectedCells()
        {
            OverrideAsymmetricBounds();

            IReadOnlyList<Vector3Int> occupiedCells =
                _structureFootprint.GetOccupiedCells(Vector3Int.zero, 90, CELL_SIZE_METERS);

            CollectionAssert.AreEqual(
                new[]
                {
                    new Vector3Int(-1, 0, 0),
                    new Vector3Int(-1, 0, 1),
                    new Vector3Int(-1, 0, 2),
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(0, 0, 1),
                    new Vector3Int(0, 0, 2),
                },
                occupiedCells);
        }

        [Test]
        public void GetOccupiedCells_AsymmetricBoundsAtOneHundredEightyDegrees_ReturnsExpectedCells()
        {
            OverrideAsymmetricBounds();

            IReadOnlyList<Vector3Int> occupiedCells =
                _structureFootprint.GetOccupiedCells(Vector3Int.zero, 180, CELL_SIZE_METERS);

            CollectionAssert.AreEqual(
                new[]
                {
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(0, 0, 1),
                    new Vector3Int(1, 0, 0),
                    new Vector3Int(1, 0, 1),
                    new Vector3Int(2, 0, 0),
                    new Vector3Int(2, 0, 1),
                },
                occupiedCells);
        }

        [Test]
        public void GetOccupiedCells_AsymmetricBoundsAtTwoHundredSeventyDegrees_ReturnsExpectedCells()
        {
            OverrideAsymmetricBounds();

            IReadOnlyList<Vector3Int> occupiedCells =
                _structureFootprint.GetOccupiedCells(Vector3Int.zero, 270, CELL_SIZE_METERS);

            CollectionAssert.AreEqual(
                new[]
                {
                    new Vector3Int(0, 0, -2),
                    new Vector3Int(0, 0, -1),
                    new Vector3Int(0, 0, 0),
                    new Vector3Int(1, 0, -2),
                    new Vector3Int(1, 0, -1),
                    new Vector3Int(1, 0, 0),
                },
                occupiedCells);
        }

        [TestCase(-90, 270)]
        [TestCase(450, 90)]
        public void GetOccupiedCells_NormalizedRotation_MatchesCanonicalRotation(
            int rotationDegrees,
            int canonicalRotationDegrees)
        {
            OverrideAsymmetricBounds();

            IReadOnlyList<Vector3Int> occupiedCells =
                _structureFootprint.GetOccupiedCells(Vector3Int.zero, rotationDegrees, CELL_SIZE_METERS);
            IReadOnlyList<Vector3Int> canonicalOccupiedCells =
                _structureFootprint.GetOccupiedCells(Vector3Int.zero, canonicalRotationDegrees, CELL_SIZE_METERS);

            CollectionAssert.AreEqual(canonicalOccupiedCells, occupiedCells);
        }

        [TestCase(0, 3f, 2f, -1f, -0.5f)]
        [TestCase(90, 2f, 3f, -0.5f, 1f)]
        [TestCase(180, 3f, 2f, 1f, 0.5f)]
        [TestCase(270, 2f, 3f, 0.5f, -1f)]
        public void ScaleAndCenterOffset_AsymmetricBounds_ReturnExpectedValues(
            int rotationDegrees,
            float expectedScaleXMeters,
            float expectedScaleZMeters,
            float expectedCenterXMeters,
            float expectedCenterZMeters)
        {
            OverrideAsymmetricBounds();

            Vector3 scaleMeters = _structureFootprint.GetScaleMeters(
                CELL_SIZE_METERS,
                rotationDegrees);
            Vector3 centerOffsetMeters = _structureFootprint.GetHorizontalCenterOffsetMeters(
                CELL_SIZE_METERS,
                rotationDegrees);

            Assert.AreEqual(new Vector3(expectedScaleXMeters, 1f, expectedScaleZMeters), scaleMeters);
            Assert.AreEqual(new Vector3(expectedCenterXMeters, 0f, expectedCenterZMeters), centerOffsetMeters);
        }

        [Test]
        public void OverrideCellBounds_InvalidBounds_PreservesPreviousValidBounds()
        {
            BoundsInt validCellBounds =
                new(new Vector3Int(-2, 0, -1), new Vector3Int(3, 1, 2));
            _structureFootprint.OverrideCellBounds(validCellBounds);
#if LOGGER_ENABLED
            LogAssert.Expect(
                LogType.Error,
                $"<color=red>[GridFootprint][{_structureFootprint.name}]</color> {_structureFootprint.name} cannot override structure footprint with invalid cell bounds 'Position: (0, 0, 0), Size: (0, 1, 1)'. All size axes must be greater than zero.");
#endif

            _structureFootprint.OverrideCellBounds(
                new BoundsInt(Vector3Int.zero, new Vector3Int(0, 1, 1)));

            Assert.AreEqual(validCellBounds, _structureFootprint.GetCellBounds(CELL_SIZE_METERS));
        }

        [Test]
        public void TryValidate_FrameworkTestFootprint_ReturnsTrue()
        {
            bool isValid = _structureFootprint.TryValidate(_structureFootprint);

            Assert.IsTrue(isValid);
        }

        [Test]
        public void GetCellBounds_DifferentCellSizes_RecalculatesMeshFootprint()
        {
            BoundsInt oneMeterCellBounds = _structureFootprint.GetCellBounds(1f);

            BoundsInt halfMeterCellBounds = _structureFootprint.GetCellBounds(0.5f);

            Assert.GreaterOrEqual(halfMeterCellBounds.size.x, oneMeterCellBounds.size.x);
            Assert.GreaterOrEqual(halfMeterCellBounds.size.z, oneMeterCellBounds.size.z);
            Assert.Greater(
                halfMeterCellBounds.size.x * halfMeterCellBounds.size.z,
                oneMeterCellBounds.size.x * oneMeterCellBounds.size.z);
        }

        #endregion

        #region Private Methods

        private void OverrideAsymmetricBounds()
        {
            _structureFootprint.OverrideCellBounds(
                new BoundsInt(
                    new Vector3Int(-2, 0, -1),
                    new Vector3Int(3, 1, 2)));
        }

        #endregion
    }
}
