using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Authoritative committed structure-placement state shared by runtime placement and save/load.
    /// </summary>
    [Serializable]
    public sealed class PlacementState
    {
        private readonly List<CommittedGridOccupantPlacement> _structures = new();

        public IReadOnlyList<CommittedGridOccupantPlacement> Structures => _structures;

        #region Public Methods

        public PlacementState Clone()
        {
            PlacementState clonedState = new();
            foreach (CommittedGridOccupantPlacement structurePlacement in _structures)
            {
                clonedState._structures.Add(structurePlacement.Clone());
            }

            return clonedState;
        }

        public void ReplaceWith(PlacementState state)
        {
            _structures.Clear();
            foreach (CommittedGridOccupantPlacement structurePlacement in state.Structures)
            {
                _structures.Add(structurePlacement.Clone());
            }
        }

        public void Clear()
        {
            _structures.Clear();
        }

        public void UpsertStructure(
            string instanceId,
            StructureSO structureSO,
            Vector3 worldPosition,
            int rotationDegrees)
        {
            CommittedGridOccupantPlacement existingPlacement = _structures.Find(
                structurePlacement => structurePlacement.InstanceId == instanceId);

            if (existingPlacement != null)
            {
                existingPlacement.SetStructure(structureSO, worldPosition, rotationDegrees);
                return;
            }

            _structures.Add(new CommittedGridOccupantPlacement(
                instanceId,
                structureSO,
                worldPosition,
                rotationDegrees));
        }

        public bool TryGetStructure(string instanceId, out StructureSO structureSO)
        {
            CommittedGridOccupantPlacement structurePlacement = _structures.Find(
                candidatePlacement => candidatePlacement.InstanceId == instanceId);

            if (structurePlacement != null)
            {
                structureSO = structurePlacement.StructureSO;
                return true;
            }

            structureSO = null;
            return false;
        }

        public void RemoveStructure(string instanceId)
        {
            _structures.RemoveAll(structurePlacement => structurePlacement.InstanceId == instanceId);
        }

        #endregion
    }

    /// <summary>
    /// Committed, save-ready placement state for one structure instance.
    /// </summary>
    [Serializable]
    public sealed class CommittedGridOccupantPlacement
    {
        [SerializeField] private string _instanceId;
        [SerializeField] private StructureSO _structureSO;
        [SerializeField] private Vector3 _worldPosition;
        [SerializeField] private int _rotationDegrees;

        public CommittedGridOccupantPlacement()
        {
        }

        public CommittedGridOccupantPlacement(
            string instanceId,
            StructureSO structureSO,
            Vector3 worldPosition,
            int rotationDegrees)
        {
            _instanceId = instanceId;
            _structureSO = structureSO;
            _worldPosition = worldPosition;
            _rotationDegrees = rotationDegrees;
        }

        public string InstanceId => _instanceId;
        public StructureSO StructureSO => _structureSO;
        public Vector3 WorldPosition => _worldPosition;
        public int RotationDegrees => _rotationDegrees;

        #region Public Methods

        public void SetStructure(
            StructureSO structureSO,
            Vector3 worldPosition,
            int rotationDegrees)
        {
            _structureSO = structureSO;
            _worldPosition = worldPosition;
            _rotationDegrees = rotationDegrees;
        }

        public CommittedGridOccupantPlacement Clone()
        {
            return new CommittedGridOccupantPlacement(
                _instanceId,
                _structureSO,
                _worldPosition,
                _rotationDegrees);
        }

        #endregion
    }
}
