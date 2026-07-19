using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Manages structure placement on the grid.
    /// It creates, moves, replaces, removes, and restores structures while keeping
    /// the saved placement state, runtime structure registry, and grid occupancy data synchronized.
    /// It also notifies other systems whenever the committed placement state changes.
    /// </summary>
    public sealed class GridOccupantPlacementService
    {
        private const int DEFAULT_ROTATION_DEGREES = 0;

        private readonly IGridPlacementGateway _gridPlacementGateway;
        private readonly PlacementState _placementState;
        private readonly GridOccupantRegistry _placedStructureRegistry;
        private readonly IGridOccupantPlacementFactory _structurePlacementFactory;

        // Internal because the parameters are internal collaborators; GridSystemInstaller composes this in-assembly.
        internal GridOccupantPlacementService(
            IGridPlacementGateway gridPlacementGateway,
            PlacementState placementState,
            GridOccupantRegistry placedStructureRegistry,
            IGridOccupantPlacementFactory structurePlacementFactory)
        {
            _gridPlacementGateway = gridPlacementGateway;
            _placementState = placementState;
            _placedStructureRegistry = placedStructureRegistry;
            _structurePlacementFactory = structurePlacementFactory;
        }

        public event Action OnCommittedStateRestored;
        public event Action<PlacementChange> OnPlacementChanged;

        #region Public Methods

        public IReadOnlyCollection<GridOccupantPlacement> GetPlacedStructures()
        {
            return _placedStructureRegistry.GetStructures();
        }

        public void ClearAllStructures()
        {
            bool hadRuntimeStructures = _placedStructureRegistry.Count > 0;

            ClearRuntimeStructures(false);
            _placementState.Clear();

            if (hadRuntimeStructures)
            {
                RaisePlacementChanged(PlacementChangeKind.Cleared, null, null);
            }
        }

        public UniTask<bool> PlaceStructureIfEmptyAsync(
            StructureSO structureSO,
            Vector3 worldPosition,
            IGridOccupantPlacementProcessor placementProcessor,
            CancellationToken cancellationToken)
        {
            return PlaceStructureIfEmptyAsync(
                structureSO,
                worldPosition,
                DEFAULT_ROTATION_DEGREES,
                placementProcessor,
                cancellationToken);
        }

        public async UniTask<bool> PlaceStructureIfEmptyAsync(
            StructureSO structureSO,
            Vector3 worldPosition,
            int rotationDegrees,
            IGridOccupantPlacementProcessor placementProcessor,
            CancellationToken cancellationToken)
        {
            string instanceId = Guid.NewGuid().ToString();
            Vector3 gridWorldPosition = _gridPlacementGateway.WorldToGridWorld(worldPosition);
            GridOccupantPlacement structurePlacement =
                await _structurePlacementFactory.CreateStructureAsync(
                    instanceId,
                    structureSO,
                    gridWorldPosition,
                    rotationDegrees,
                    cancellationToken,
                    $"Failed to load structure prefab for '{structureSO.Id}'.",
                    placementProcessor);

            if (structurePlacement == null)
            {
                return false;
            }

            if (!_gridPlacementGateway.CanOccupy(structurePlacement.Footprint, gridWorldPosition, rotationDegrees))
            {
                Echo.Log("Structure placement was rejected because its grid footprint is occupied or outside the grid.");
                _structurePlacementFactory.DestroyStructure(structurePlacement);
                return false;
            }

            CommitRuntimeStructure(structurePlacement, gridWorldPosition, rotationDegrees);
            RaisePlacementChanged(PlacementChangeKind.Created, structurePlacement, structurePlacement.InstanceId);
            return true;
        }

        public async UniTask<bool> ReplaceStructureAsync(
            string instanceId,
            StructureSO replacementStructureSO,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                Echo.Warning("Cannot replace structure because instance id is missing.");
                return false;
            }

            if (!replacementStructureSO)
            {
                Echo.Warning($"Cannot replace structure '{instanceId}' because replacement structure is missing.");
                return false;
            }

            if (!_placedStructureRegistry.TryGet(instanceId, out GridOccupantPlacement currentPlacement))
            {
                Echo.Warning($"Cannot replace structure '{instanceId}' because no runtime placement exists.");
                return false;
            }

            Vector3 gridWorldPosition = _gridPlacementGateway.WorldToGridWorld(currentPlacement.WorldPosition);
            int rotationDegrees = currentPlacement.RotationDegrees;
            GridOccupantPlacement replacementPlacement =
                await _structurePlacementFactory.CreateStructureAsync(
                    instanceId,
                    replacementStructureSO,
                    gridWorldPosition,
                    rotationDegrees,
                    cancellationToken,
                    $"Failed to load replacement structure prefab for '{replacementStructureSO.Id}'.");

            if (replacementPlacement == null)
            {
                return false;
            }

            if (!_gridPlacementGateway.CanOccupy(
                    replacementPlacement.Footprint,
                    gridWorldPosition,
                    rotationDegrees,
                    currentPlacement.InstanceId))
            {
                Echo.Log("Replacement structure was rejected because its grid footprint is occupied or outside the grid.");
                _structurePlacementFactory.DestroyStructure(replacementPlacement);
                return false;
            }

            _placedStructureRegistry.Remove(currentPlacement.InstanceId);
            _structurePlacementFactory.DestroyStructure(currentPlacement);
            CommitRuntimeStructure(replacementPlacement, gridWorldPosition, rotationDegrees);
            RaisePlacementChanged(PlacementChangeKind.Replaced, replacementPlacement, replacementPlacement.InstanceId);
            return true;
        }

        public bool TryPickUpStructure(Vector3 worldPosition, out GridOccupantPlacement heldStructurePlacement)
        {
            return TryDetachRuntimeStructure(
                worldPosition,
                PlacementChangeKind.Removed,
                out heldStructurePlacement);
        }

        public bool TryPlaceHeldStructure(GridOccupantPlacement heldStructurePlacement, Vector3 worldPosition)
        {
            return TryPlaceHeldStructure(heldStructurePlacement, worldPosition, PlacementChangeKind.Moved);
        }

        public bool RestoreHeldStructure(GridOccupantPlacement heldStructurePlacement)
        {
            if (heldStructurePlacement == null)
            {
                Echo.Warning("Cannot restore held structure because no held structure was provided.");
                return false;
            }

            if (TryPlaceHeldStructure(
                    heldStructurePlacement,
                    heldStructurePlacement.OriginalWorldPosition,
                    PlacementChangeKind.Restored))
            {
                return true;
            }

            Echo.Error($"Failed to restore held structure '{heldStructurePlacement.InstanceId}' to its original position.");
            return false;
        }

        public bool DestroyStructure(Vector3 worldPosition)
        {
            if (!TryDetachRuntimeStructure(
                    worldPosition,
                    null,
                    out GridOccupantPlacement removedStructurePlacement))
            {
                return false;
            }

            _structurePlacementFactory.DestroyStructure(removedStructurePlacement);
            _placementState.RemoveStructure(removedStructurePlacement.InstanceId);
            RaisePlacementChanged(PlacementChangeKind.Removed, null, removedStructurePlacement.InstanceId);
            return true;
        }

        public StructureSO RemoveStructure(Vector3 worldPosition)
        {
            StructureSO structureSO = GetStructureSOAtPosition(worldPosition);
            return DestroyStructure(worldPosition) ? structureSO : null;
        }

        public StructureSO GetStructureSOAtPosition(Vector3 worldPosition)
        {
            return TryGetStructureSOAtPosition(worldPosition, out StructureSO structureSO)
                ? structureSO
                : null;
        }

        public bool TryGetStructurePosition(Vector3 worldPosition, out Vector3 structureWorldPosition)
        {
            if (TryGetInstanceIdAtPosition(worldPosition, out string instanceId) &&
                _placedStructureRegistry.TryGet(instanceId, out GridOccupantPlacement structurePlacement))
            {
                structureWorldPosition = structurePlacement.WorldPosition;
                return true;
            }

            Echo.Error("Cannot get structure position because no structure occupies the selected grid cell.");
            structureWorldPosition = Vector3.zero;
            return false;
        }

        public Vector3 GetStructurePosition(Vector3 worldPosition)
        {
            return TryGetStructurePosition(worldPosition, out Vector3 structureWorldPosition)
                ? structureWorldPosition
                : Vector3.zero;
        }

        public bool TryGetInstanceIdAtPosition(Vector3 worldPosition, out string instanceId)
        {
            return _gridPlacementGateway.TryGetInstanceIdAtPosition(worldPosition, out instanceId);
        }

        public bool TryGetPlacement(string instanceId, out GridOccupantPlacement structurePlacement)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                Echo.Warning("Cannot get structure placement because instance id is missing.");
                structurePlacement = null;
                return false;
            }

            return _placedStructureRegistry.TryGet(instanceId, out structurePlacement);
        }

        public bool TryGetPlacementAtPosition(Vector3 worldPosition, out GridOccupantPlacement structurePlacement)
        {
            structurePlacement = null;
            return TryGetInstanceIdAtPosition(worldPosition, out string instanceId) &&
                   _placedStructureRegistry.TryGet(instanceId, out structurePlacement);
        }

        public async UniTask RestoreCommittedStateAsync(CancellationToken cancellationToken)
        {
            ClearRuntimeStructures(false);

            foreach (CommittedGridOccupantPlacement structurePlacement in _placementState.Structures)
            {
                if (!structurePlacement.StructureSO)
                {
                    Echo.Warning($"Cannot restore placed structure '{structurePlacement.InstanceId}' because its resolved structure is missing.");
                    continue;
                }

                try
                {
                    await RestoreStructureAsync(
                        structurePlacement,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Echo.Log("Placement restore was canceled because the placement system was destroyed.");
                    return;
                }
            }

            OnCommittedStateRestored?.Invoke();
        }

        public void ClearRuntimeStructures(bool shouldRaiseRemovedEvent)
        {
            bool hadRuntimeStructures = _placedStructureRegistry.Count > 0;

            foreach (GridOccupantPlacement structurePlacement in _placedStructureRegistry.Structures)
            {
                _structurePlacementFactory.DestroyStructure(structurePlacement);
            }

            _placedStructureRegistry.Clear();
            RebuildOccupancyIndex();

            if (shouldRaiseRemovedEvent && hadRuntimeStructures)
            {
                RaisePlacementChanged(PlacementChangeKind.Cleared, null, null);
            }
        }

        #endregion

        #region Private Methods

        private bool TryPlaceHeldStructure(
            GridOccupantPlacement heldStructurePlacement,
            Vector3 worldPosition,
            PlacementChangeKind placementChangeKind)
        {
            if (heldStructurePlacement == null)
            {
                Echo.Warning("Cannot place held structure because no held structure was provided.");
                return false;
            }

            Vector3 gridWorldPosition = _gridPlacementGateway.WorldToGridWorld(worldPosition);
            int rotationDegrees = heldStructurePlacement.RotationDegrees;
            if (!_gridPlacementGateway.CanOccupy(
                    heldStructurePlacement.Footprint,
                    gridWorldPosition,
                    rotationDegrees,
                    heldStructurePlacement.InstanceId))
            {
                Echo.Log("Held structure placement was rejected because its grid footprint is occupied or outside the grid.");
                return false;
            }

            heldStructurePlacement.PlaceAt(gridWorldPosition, rotationDegrees);
            CommitRuntimeStructure(heldStructurePlacement, gridWorldPosition, rotationDegrees);
            RaisePlacementChanged(placementChangeKind, heldStructurePlacement, heldStructurePlacement.InstanceId);
            return true;
        }

        private bool TryDetachRuntimeStructure(
            Vector3 worldPosition,
            PlacementChangeKind? placementChangeKind,
            out GridOccupantPlacement detachedStructurePlacement)
        {
            if (!TryGetInstanceIdAtPosition(worldPosition, out string instanceId))
            {
                Echo.Log("Cannot pick up structure because no structure occupies the selected grid cell.");
                detachedStructurePlacement = null;
                return false;
            }

            if (!_placedStructureRegistry.TryGet(instanceId, out GridOccupantPlacement structurePlacement))
            {
                Echo.Error($"Cannot pick up structure '{instanceId}' because its runtime records are incomplete.");
                detachedStructurePlacement = null;
                return false;
            }

            _placedStructureRegistry.Remove(instanceId);
            structurePlacement.MarkHeldAtCurrentPosition();
            RebuildOccupancyIndex();

            detachedStructurePlacement = structurePlacement;
            if (placementChangeKind.HasValue)
            {
                RaisePlacementChanged(placementChangeKind.Value, structurePlacement, instanceId);
            }

            return true;
        }

        private async UniTask RestoreStructureAsync(
            CommittedGridOccupantPlacement structurePlacement,
            CancellationToken cancellationToken)
        {
            Vector3 gridWorldPosition = _gridPlacementGateway.WorldToGridWorld(structurePlacement.WorldPosition);
            GridOccupantPlacement runtimePlacement =
                await _structurePlacementFactory.CreateStructureAsync(
                    structurePlacement.InstanceId,
                    structurePlacement.StructureSO,
                    gridWorldPosition,
                    structurePlacement.RotationDegrees,
                    cancellationToken,
                    $"Failed to restore saved structure '{structurePlacement.InstanceId}' for '{structurePlacement.StructureSO.Id}'.");

            if (runtimePlacement == null)
            {
                return;
            }

            if (!_gridPlacementGateway.CanOccupy(
                    runtimePlacement.Footprint,
                    gridWorldPosition,
                    structurePlacement.RotationDegrees))
            {
                Echo.Error($"Cannot restore saved structure '{structurePlacement.InstanceId}' because its grid space is occupied or outside the grid.");
                _structurePlacementFactory.DestroyStructure(runtimePlacement);
                return;
            }

            CommitRuntimeStructure(runtimePlacement, gridWorldPosition, structurePlacement.RotationDegrees);
            RaisePlacementChanged(PlacementChangeKind.Restored, runtimePlacement, runtimePlacement.InstanceId);
        }

        private void CommitRuntimeStructure(
            GridOccupantPlacement structurePlacement,
            Vector3 gridWorldPosition,
            int rotationDegrees)
        {
            _placedStructureRegistry.Upsert(structurePlacement);
            _placementState.UpsertStructure(
                structurePlacement.InstanceId,
                structurePlacement.StructureSO,
                gridWorldPosition,
                rotationDegrees);
            RebuildOccupancyIndex();
        }

        private void RebuildOccupancyIndex()
        {
            _gridPlacementGateway.RebuildOccupancyIndex(_placedStructureRegistry.GetStructures());
        }

        private void RaisePlacementChanged(
            PlacementChangeKind kind,
            GridOccupantPlacement structurePlacement,
            string instanceId)
        {
            OnPlacementChanged?.Invoke(new PlacementChange(
                kind,
                structurePlacement,
                instanceId));
        }

        private bool TryGetStructureSOAtPosition(Vector3 worldPosition, out StructureSO structureSO)
        {
            if (TryGetInstanceIdAtPosition(worldPosition, out string instanceId) &&
                _placementState.TryGetStructure(instanceId, out structureSO))
            {
                return true;
            }

            structureSO = null;
            return false;
        }

        #endregion
    }
}
