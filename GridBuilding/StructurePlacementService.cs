using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace FakeMG.GridBuilding
{
    /// <summary>
    /// Coordinates structure placement, movement, destruction, restore, and grid/state registration.
    /// </summary>
    internal sealed class StructurePlacementService
    {
        private readonly GridManager _gridManager;
        private readonly PlacementState _placementState;
        private readonly PlacedStructureRegistry _placedStructureRegistry;
        private readonly StructureInstanceFactory _structureInstanceFactory;
        private readonly bool _enableLogging;
        private readonly UnityEngine.Object _logContext;

        public StructurePlacementService(
            GridManager gridManager,
            PlacementState placementState,
            PlacedStructureRegistry placedStructureRegistry,
            StructureInstanceFactory structureInstanceFactory,
            bool enableLogging,
            UnityEngine.Object logContext)
        {
            _gridManager = gridManager;
            _placementState = placementState;
            _placedStructureRegistry = placedStructureRegistry;
            _structureInstanceFactory = structureInstanceFactory;
            _enableLogging = enableLogging;
            _logContext = logContext;
        }

        public event Action OnPlaced;
        public event Action OnRemoved;

        #region Public Methods

        public IReadOnlyCollection<StructurePlacement> GetPlacedStructures()
        {
            return _placedStructureRegistry.GetStructures();
        }

        public void ClearAllStructures()
        {
            ClearRuntimeStructures(true);
            _placementState.Clear();
        }

        public async UniTask<bool> PlaceStructureIfEmptyAsync(
            StructureSO structureSO,
            Vector3 worldPosition,
            CancellationToken cancellationToken)
        {
            string instanceId = Guid.NewGuid().ToString();
            Vector3 gridWorldPosition = _gridManager.WorldToGridWorld(worldPosition);
            StructurePlacement structurePlacement =
                await _structureInstanceFactory.CreateStructureAsync(
                    instanceId,
                    structureSO,
                    gridWorldPosition,
                    cancellationToken,
                    $"Failed to load structure prefab for '{structureSO.Id}'.");

            if (structurePlacement == null)
            {
                return false;
            }

            if (!_gridManager.IsEmptyGridSpace(structurePlacement.StructureInstance, gridWorldPosition))
            {
                Echo.Log("Structure placement was rejected because its grid footprint is occupied or outside the grid.", _enableLogging, _logContext);
                _structureInstanceFactory.DestroyStructure(structurePlacement);
                return false;
            }

            if (TryCommitNewStructure(structurePlacement, gridWorldPosition))
            {
                return true;
            }

            _structureInstanceFactory.DestroyStructure(structurePlacement);
            return false;
        }

        public bool TryPickUpStructure(Vector3 worldPosition, out StructurePlacement heldStructurePlacement)
        {
            if (!TryGetInstanceIdAtPosition(worldPosition, out string instanceId))
            {
                Echo.Log("Cannot pick up structure because no structure occupies the selected grid cell.", _enableLogging, _logContext);
                heldStructurePlacement = null;
                return false;
            }

            if (!_placedStructureRegistry.TryGet(instanceId, out StructurePlacement structurePlacement))
            {
                Echo.Error($"Cannot pick up structure '{instanceId}' because its placement records are incomplete.", _enableLogging, _logContext);
                heldStructurePlacement = null;
                return false;
            }

            if (!_gridManager.TryRemoveStructure(worldPosition, out string removedInstanceId) ||
                removedInstanceId != instanceId)
            {
                Echo.Error($"Cannot pick up structure '{instanceId}' because its grid record could not be removed consistently.", _enableLogging, _logContext);
                heldStructurePlacement = null;
                return false;
            }

            _placedStructureRegistry.Remove(instanceId);

            structurePlacement.MarkHeldAtCurrentPosition();
            heldStructurePlacement = structurePlacement;

            OnRemoved?.Invoke();
            return true;
        }

        public bool TryPlaceHeldStructure(StructurePlacement heldStructurePlacement, Vector3 worldPosition)
        {
            if (heldStructurePlacement == null)
            {
                Echo.Warning("Cannot place held structure because no held structure was provided.", _enableLogging, _logContext);
                return false;
            }

            Vector3 gridWorldPosition = _gridManager.WorldToGridWorld(worldPosition);
            if (!_gridManager.IsEmptyGridSpace(
                    heldStructurePlacement.StructureInstance,
                    gridWorldPosition,
                    heldStructurePlacement.WorldPosition))
            {
                Echo.Log("Held structure placement was rejected because its grid footprint is occupied or outside the grid.", _enableLogging, _logContext);
                return false;
            }

            if (!TryCommitHeldStructure(heldStructurePlacement, gridWorldPosition))
            {
                return false;
            }

            heldStructurePlacement.PlaceAt(gridWorldPosition);
            OnPlaced?.Invoke();
            return true;
        }

        public bool RestoreHeldStructure(StructurePlacement heldStructurePlacement)
        {
            if (heldStructurePlacement == null)
            {
                Echo.Warning("Cannot restore held structure because no held structure was provided.", _enableLogging, _logContext);
                return false;
            }

            if (TryPlaceHeldStructure(
                    heldStructurePlacement,
                    heldStructurePlacement.OriginalWorldPosition))
            {
                return true;
            }

            Echo.Error(
                $"Failed to restore held structure '{heldStructurePlacement.InstanceId}' to its original position.",
                _enableLogging,
                _logContext);
            return false;
        }

        public bool DestroyStructure(Vector3 worldPosition)
        {
            if (!TryPickUpStructure(worldPosition, out StructurePlacement removedStructurePlacement))
            {
                return false;
            }

            _structureInstanceFactory.DestroyStructure(removedStructurePlacement);
            _placementState.RemoveStructure(removedStructurePlacement.InstanceId);
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
                _placedStructureRegistry.TryGet(instanceId, out StructurePlacement structurePlacement))
            {
                structureWorldPosition = structurePlacement.WorldPosition;
                return true;
            }

            Echo.Error("Cannot get structure position because no structure occupies the selected grid cell.", _enableLogging, _logContext);
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
            Vector3Int cellPosition = _gridManager.WorldToCell(worldPosition);
            if (_gridManager.GridData.TryGetValue(cellPosition, out PlacementData placementData))
            {
                instanceId = placementData.InstanceID;
                return true;
            }

            instanceId = null;
            return false;
        }

        public async UniTask RestoreCommittedStateAsync(CancellationToken cancellationToken)
        {
            ClearRuntimeStructures(false);

            foreach (CommittedStructurePlacement structurePlacement in _placementState.Structures)
            {
                if (!structurePlacement.StructureSO)
                {
                    Echo.Warning($"Cannot restore placed structure '{structurePlacement.InstanceId}' because its resolved structure is missing.", _enableLogging, _logContext);
                    continue;
                }

                try
                {
                    await RestoreStructureAsync(
                        structurePlacement,
                        structurePlacement.StructureSO,
                        cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Echo.Log("Placement restore was canceled because the placement system was destroyed.", _enableLogging, _logContext);
                    return;
                }
            }
        }

        public void ClearRuntimeStructures(bool shouldRaiseRemovedEvent)
        {
            bool hadRuntimeStructures = _placedStructureRegistry.Count > 0;

            foreach (StructurePlacement structurePlacement in _placedStructureRegistry.Structures)
            {
                _structureInstanceFactory.DestroyStructure(structurePlacement);
            }

            _placedStructureRegistry.Clear();
            _gridManager.Clear();

            if (shouldRaiseRemovedEvent && hadRuntimeStructures)
            {
                OnRemoved?.Invoke();
            }
        }

        #endregion

        #region Private Methods

        private bool TryCommitNewStructure(
            StructurePlacement structurePlacement,
            Vector3 gridWorldPosition)
        {
            if (!TryRegisterRuntimeStructure(structurePlacement, gridWorldPosition, null))
            {
                return false;
            }

            _placementState.UpsertStructure(
                structurePlacement.InstanceId,
                structurePlacement.StructureSO,
                gridWorldPosition);
            OnPlaced?.Invoke();
            return true;
        }

        private bool TryCommitHeldStructure(
            StructurePlacement heldStructurePlacement,
            Vector3 gridWorldPosition)
        {
            if (!TryRegisterRuntimeStructure(
                    heldStructurePlacement,
                    gridWorldPosition,
                    heldStructurePlacement.WorldPosition))
            {
                return false;
            }

            _placementState.UpsertStructure(
                heldStructurePlacement.InstanceId,
                heldStructurePlacement.StructureSO,
                gridWorldPosition);
            return true;
        }

        private async UniTask RestoreStructureAsync(
            CommittedStructurePlacement structurePlacement,
            StructureSO structureSO,
            CancellationToken cancellationToken)
        {
            Vector3 gridWorldPosition = _gridManager.WorldToGridWorld(structurePlacement.WorldPosition);
            StructurePlacement runtimePlacement =
                await _structureInstanceFactory.CreateStructureAsync(
                    structurePlacement.InstanceId,
                    structureSO,
                    gridWorldPosition,
                    cancellationToken,
                    $"Failed to restore saved structure '{structurePlacement.InstanceId}' for '{structureSO.Id}'.");

            if (runtimePlacement == null)
            {
                return;
            }

            if (!_gridManager.IsEmptyGridSpace(runtimePlacement.StructureInstance, gridWorldPosition))
            {
                Echo.Error($"Cannot restore saved structure '{structurePlacement.InstanceId}' because its grid space is occupied or outside the grid.", _enableLogging, _logContext);
                _structureInstanceFactory.DestroyStructure(runtimePlacement);
                return;
            }

            if (!TryRegisterRuntimeStructure(
                    runtimePlacement,
                    gridWorldPosition,
                    null))
            {
                _structureInstanceFactory.DestroyStructure(runtimePlacement);
            }
        }

        private bool TryRegisterRuntimeStructure(
            StructurePlacement structurePlacement,
            Vector3 gridWorldPosition,
            Vector3? oldWorldPosition)
        {
            if (_placedStructureRegistry.Contains(structurePlacement.InstanceId))
            {
                Echo.Error($"Cannot register structure '{structurePlacement.InstanceId}' because its runtime records already exist.", _enableLogging, _logContext);
                return false;
            }

            _gridManager.RegisterStructure(
                structurePlacement.StructureInstance,
                structurePlacement.InstanceId,
                gridWorldPosition,
                oldWorldPosition);

            _placedStructureRegistry.Add(structurePlacement);

            return true;
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
