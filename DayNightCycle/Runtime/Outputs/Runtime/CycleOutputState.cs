using System.Collections.Generic;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Stores the current evaluated value for every active output key.
    /// </summary>
    internal sealed class CycleOutputState : IReadOnlyCycleOutputState
    {
        private readonly Dictionary<CycleOutputKeySO, object> _valueByOutputKey = new();

        #region Public Methods

        public bool TryGetValue(BoolCycleOutputKeySO outputKeySO, out bool value)
        {
            return TryGetTypedValue(outputKeySO, out value);
        }

        public bool TryGetValue(IntCycleOutputKeySO outputKeySO, out int value)
        {
            return TryGetTypedValue(outputKeySO, out value);
        }

        public bool TryGetValue(FloatCycleOutputKeySO outputKeySO, out float value)
        {
            return TryGetTypedValue(outputKeySO, out value);
        }

        public bool TryGetValue(ColorCycleOutputKeySO outputKeySO, out UnityEngine.Color value)
        {
            return TryGetTypedValue(outputKeySO, out value);
        }

        public bool TryGetValue(
            RotationCycleOutputKeySO outputKeySO,
            out UnityEngine.Quaternion value)
        {
            return TryGetTypedValue(outputKeySO, out value);
        }

        internal void ReplaceValues(IEnumerable<CycleOutputRuntimeEntry> entries)
        {
            _valueByOutputKey.Clear();
            foreach (CycleOutputRuntimeEntry entry in entries)
            {
                _valueByOutputKey[entry.Definition.OutputKeySO] = entry.CurrentValue;
            }
        }

        #endregion

        #region Private Methods

        private bool TryGetTypedValue<T>(CycleOutputKeySO outputKeySO, out T value)
        {
            if (_valueByOutputKey.TryGetValue(outputKeySO, out object untypedValue)
                && untypedValue is T typedValue)
            {
                value = typedValue;
                return true;
            }

            value = default;
            return false;
        }

        #endregion
    }
}
