using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Provides stable asset identity and value-type metadata for one cycle output.
    /// </summary>
    public abstract class CycleOutputKeySO : ScriptableObject
    {
        [SerializeField, HideInInspector] private string _legacyId;

        public abstract Type ValueType { get; }

#if UNITY_EDITOR
        public string LegacyId => _legacyId;

        #region Public Methods

        public void ConfigureLegacyIdForEditor(string legacyId)
        {
            _legacyId = legacyId;
        }

        #endregion
#endif
    }
}
