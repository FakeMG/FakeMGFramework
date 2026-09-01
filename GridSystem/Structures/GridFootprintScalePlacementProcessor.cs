using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Applies canonical cell bounds and their runtime-derived dimensions before grid validation.
    /// </summary>
    public sealed class GridFootprintScalePlacementProcessor : IGridOccupantPlacementProcessor
    {
        private readonly BoundsInt _cellBounds;
        private readonly float _cellSizeMeters;

        public GridFootprintScalePlacementProcessor(BoundsInt cellBounds, float cellSizeMeters)
        {
            _cellBounds = cellBounds;
            _cellSizeMeters = cellSizeMeters;
        }

        #region Public Methods

        public void Process(GameObject structureInstance)
        {
            if (!structureInstance.TryGetComponent(out GridFootprint structureFootprint))
            {
                Echo.Warning(
                    $"Cannot apply saved footprint to '{structureInstance.name}' because it has no {nameof(GridFootprint)}.",
                    structureInstance);
                return;
            }

            if (_cellSizeMeters <= 0f)
            {
                Echo.Error(
                    $"Cannot apply saved footprint to '{structureInstance.name}' because grid cell size '{_cellSizeMeters}' is not positive.",
                    context: structureInstance);
                return;
            }

            structureFootprint.OverrideCellBounds(_cellBounds);
            ApplyRuntimeDimensions(structureInstance, structureFootprint);
        }

        #endregion

        #region Private Methods

        private void ApplyRuntimeDimensions(GameObject structureInstance, GridFootprint structureFootprint)
        {
            Vector3 footprintScaleMeters = structureFootprint.GetScaleMeters(_cellSizeMeters, 0);
            Vector3 horizontalCenterOffsetMeters = structureFootprint.GetHorizontalCenterOffsetMeters(_cellSizeMeters, 0);

            MonoBehaviour[] behaviours = structureInstance.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour behaviour in behaviours)
            {
                if (behaviour is IGridFootprintScaleReceiver footprintScaleReceiver)
                {
                    footprintScaleReceiver.SetFootprintScaleMeters(footprintScaleMeters);
                    footprintScaleReceiver.SetFootprintHorizontalCenterOffsetMeters(horizontalCenterOffsetMeters);
                }
            }
        }

        #endregion
    }
}
