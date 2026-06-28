using System.Collections.Generic;

namespace FakeMG.GridBuilding
{
    /// <summary>
    /// Keeps placed structure runtime records keyed by their stable placement instance id.
    /// </summary>
    internal sealed class PlacedStructureRegistry
    {
        private readonly Dictionary<string, StructurePlacement> _placedStructures = new();

        public int Count => _placedStructures.Count;
        public IEnumerable<StructurePlacement> Structures => _placedStructures.Values;

        #region Public Methods

        public bool Contains(string instanceId)
        {
            return _placedStructures.ContainsKey(instanceId);
        }

        public void Add(StructurePlacement structurePlacement)
        {
            _placedStructures.Add(structurePlacement.InstanceId, structurePlacement);
        }

        public bool TryGet(string instanceId, out StructurePlacement structurePlacement)
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

        public IReadOnlyCollection<StructurePlacement> GetStructures()
        {
            return new List<StructurePlacement>(_placedStructures.Values);
        }

        #endregion
    }
}
