using System;
using FakeMG.Framework.SaveLoad.Advanced;
using UnityEditor;
using UnityEngine;

namespace FakeMG.Framework.SaveLoad
{
    public abstract class Saveable : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField] private string uniqueId;

        private void Start()
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                uniqueId = Guid.NewGuid().ToString();
#if UNITY_EDITOR
                EditorUtility.SetDirty(this); // Mark as dirty to save the new ID
#endif
            }

            SaveLoadSystem.Instance.RegisterSaveable(this, uniqueId);
        }

        private void OnDestroy()
        {
            SaveLoadSystem.Instance.UnregisterSaveable(uniqueId);
        }

        public abstract object CaptureState();
        public abstract void RestoreState(object data);
    }
}