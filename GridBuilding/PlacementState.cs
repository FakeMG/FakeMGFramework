using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.GridBuilding
{
    /// <summary>
    /// Authoritative committed structure-placement state shared by runtime placement and save/load.
    /// </summary>
    [Serializable]
    public sealed class PlacementState
    {
        public List<CommittedStructurePlacement> Structures = new();

        public PlacementState Clone()
        {
            PlacementState clonedState = new();
            foreach (CommittedStructurePlacement structurePlacement in Structures)
            {
                clonedState.Structures.Add(structurePlacement.Clone());
            }

            return clonedState;
        }

        public void ReplaceWith(PlacementState state)
        {
            Structures.Clear();
            foreach (CommittedStructurePlacement structurePlacement in state.Structures)
            {
                Structures.Add(structurePlacement.Clone());
            }
        }

        public void Clear()
        {
            Structures.Clear();
        }

        public void UpsertStructure(
            string instanceId,
            StructureSO structureSO,
            Vector3 worldPosition)
        {
            CommittedStructurePlacement existingPlacement = Structures.Find(
                structurePlacement => structurePlacement.InstanceId == instanceId);

            if (existingPlacement != null)
            {
                existingPlacement.StructureSO = structureSO;
                existingPlacement.WorldPosition = worldPosition;
                return;
            }

            Structures.Add(new CommittedStructurePlacement(
                instanceId,
                structureSO,
                worldPosition));
        }

        public bool TryGetStructure(string instanceId, out StructureSO structureSO)
        {
            CommittedStructurePlacement structurePlacement = Structures.Find(
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
            Structures.RemoveAll(structurePlacement => structurePlacement.InstanceId == instanceId);
        }
    }

    /// <summary>
    /// Committed, save-ready placement state for one structure instance.
    /// </summary>
    [Serializable]
    public sealed class CommittedStructurePlacement
    {
        public string InstanceId;
        public StructureSO StructureSO;
        public Vector3 WorldPosition;

        public CommittedStructurePlacement()
        {
        }

        public CommittedStructurePlacement(
            string instanceId,
            StructureSO structureSO,
            Vector3 worldPosition)
        {
            InstanceId = instanceId;
            StructureSO = structureSO;
            WorldPosition = worldPosition;
        }

        public CommittedStructurePlacement Clone()
        {
            return new CommittedStructurePlacement(
                InstanceId,
                StructureSO,
                WorldPosition);
        }
    }
}
