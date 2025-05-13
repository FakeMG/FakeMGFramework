using System;
using FakeMG.FakeMGFramework.SaveLoad.Advanced;
using UnityEngine;

namespace FakeMG.FakeMGFramework.SaveLoad {
    public abstract class Saveable : MonoBehaviour {
        [HideInInspector]
        [SerializeField] private string uniqueId;
        
        private void Start() {
            if (string.IsNullOrEmpty(uniqueId)) {
                uniqueId = Guid.NewGuid().ToString();
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this); // Mark as dirty to save the new ID
#endif
            }

            SaveLoadSystem.Instance.RegisterSaveable(this, uniqueId);
        }
        
        private void OnDestroy() {
            SaveLoadSystem.Instance.UnregisterSaveable(uniqueId);
        }

        public abstract object CaptureState();
        public abstract void RestoreState(object data);
    }
}