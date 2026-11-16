using System;
using UnityEngine;

namespace FakeMG.TimeCycle
{
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
