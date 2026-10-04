using System.Collections.Generic;

namespace FakeMG.GridSystem
{
    /// <summary>
    /// Keeps placed structure runtime records keyed by their stable placement instance id.
    /// </summary>
    internal sealed class GridOccupantRegistry
    {
        private readonly Dictionary<string, GridOccupantPlacement> _placedStructures = new();

        public int Count => _placedStructures.Count;
        public IEnumerable<GridOccupantPlacement> Structures => _placedStructures.Values;

        #region Public Methods

        public bool Contains(string instanceId)
        {
            return _placedStructures.ContainsKey(instanceId);
        }

        public void Add(GridOccupantPlacement structurePlacement)
        {
            _placedStructures.Add(structurePlacement.InstanceId, structurePlacement);
        }

        public void Upsert(GridOccupantPlacement structurePlacement)
        {
            _placedStructures[structurePlacement.InstanceId] = structurePlacement;
        }

        public bool TryGet(string instanceId, out GridOccupantPlacement structurePlacement)
        {
            return _placedStructures.TryGetValue(instanceId, out structurePlacement);
        }

        public bool Remove(string instanceId)
        {
            return _placedStructures.Remove(instanceId);
        }

        public void Clear()
        {
            _placedStructures.Clear();
        }

        public IReadOnlyCollection<GridOccupantPlacement> GetStructures()
        {
            return new List<GridOccupantPlacement>(_placedStructures.Values);
        }

        #endregion
    }
}
