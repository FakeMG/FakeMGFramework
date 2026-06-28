using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

namespace FakeMG.GridBuilding
{
    /// <summary>
    /// Unity-facing facade for placement input projection and structure placement operations.
    /// </summary>
    public class PlacementSystem : MonoBehaviour
    {
        [SerializeField] private GridManager _gridManager;
        [SerializeField] private LayerMask _placementLayerMask;
        [SerializeField] private bool _enableLogging = true;

        private StructurePlacementService _placementService;
        private GridPointerProjector _gridPointerProjector;

        public event Action OnPlaced;
        public event Action OnRemoved;

        #region Unity Lifecycle

        private void Start()
        {
            _gridPointerProjector = new GridPointerProjector(_gridManager, _placementLayerMask, Camera.main);

            _placementService
                .RestoreCommittedStateAsync(this.GetCancellationTokenOnDestroy())
                .Forget();
        }

        private void OnDestroy()
        {
            _placementService?.ClearRuntimeStructures(false);
        }

        #endregion

        #region Public Methods

        [Inject]
        public void Construct(PlacementState placementState)
        {
            StructureInstanceFactory structureInstanceFactory = new(
                _enableLogging,
                this);

            _placementService = new StructurePlacementService(
                _gridManager,
                placementState,
                new PlacedStructureRegistry(),
                structureInstanceFactory,
                _enableLogging,
                this);
            _placementService.OnPlaced += RaisePlacedEvent;
            _placementService.OnRemoved += RaiseRemovedEvent;
        }

        public IReadOnlyCollection<StructurePlacement> GetPlacedStructures()
        {
            return _placementService.GetPlacedStructures();
        }

        public void ClearAllStructures()
        {
            _placementService.ClearAllStructures();
        }

        public bool TryGetGridWorldPosition(
            Vector2 pointerPositionPixels,
            out Vector3 gridWorldPosition,
            out RaycastHit hitInfo)
        {
            return _gridPointerProjector.TryGetGridWorldPosition(
                pointerPositionPixels,
                out gridWorldPosition,
                out hitInfo);
        }

        public UniTask<bool> PlaceStructureIfEmptyAsync(StructureSO structureSO, Vector3 worldPosition)
        {
            return _placementService.PlaceStructureIfEmptyAsync(
                structureSO,
                worldPosition,
                this.GetCancellationTokenOnDestroy());
        }

        public bool TryPickUpStructure(Vector3 worldPosition, out StructurePlacement heldStructurePlacement)
        {
            return _placementService.TryPickUpStructure(worldPosition, out heldStructurePlacement);
        }

        public bool TryPlaceHeldStructure(StructurePlacement heldStructurePlacement, Vector3 worldPosition)
        {
            return _placementService.TryPlaceHeldStructure(heldStructurePlacement, worldPosition);
        }

        public bool RestoreHeldStructure(StructurePlacement heldStructurePlacement)
        {
            return _placementService.RestoreHeldStructure(heldStructurePlacement);
        }

        public bool DestroyStructure(Vector3 worldPosition)
        {
            return _placementService.DestroyStructure(worldPosition);
        }

        public StructureSO RemoveStructure(Vector3 worldPosition)
        {
            return _placementService.RemoveStructure(worldPosition);
        }

        public StructureSO GetStructureSOAtPosition(Vector3 worldPosition)
        {
            return _placementService.GetStructureSOAtPosition(worldPosition);
        }

        public bool TryGetStructurePosition(Vector3 worldPosition, out Vector3 structureWorldPosition)
        {
            return _placementService.TryGetStructurePosition(worldPosition, out structureWorldPosition);
        }

        public Vector3 GetStructurePosition(Vector3 worldPosition)
        {
            return _placementService.GetStructurePosition(worldPosition);
        }

        public bool TryGetInstanceIdAtPosition(Vector3 worldPosition, out string instanceId)
        {
            return _placementService.TryGetInstanceIdAtPosition(worldPosition, out instanceId);
        }

        #endregion

        #region Private Methods

        private void RaisePlacedEvent()
        {
            OnPlaced?.Invoke();
        }

        private void RaiseRemovedEvent()
        {
            OnRemoved?.Invoke();
        }

        #endregion
    }
}
