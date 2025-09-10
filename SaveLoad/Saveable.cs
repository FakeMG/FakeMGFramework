using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework.SaveLoad
{
    public abstract class Saveable : MonoBehaviour
    {
        [ReadOnly]
        [SerializeField] private string uniqueId;

        private object _cachedData;

        private void Reset()
        {
            if (string.IsNullOrEmpty(uniqueId))
            {
                uniqueId = Guid.NewGuid().ToString();
                Debug.LogWarning($"Saveable {name} has no unique ID. Generated one: {uniqueId}");
            }
        }

        public string GetUniqueId()
        {
            return uniqueId;
        }

        public abstract object CaptureState();
        public abstract void RestoreState(object data);
        public abstract void RestoreDefaultState();
    }
}