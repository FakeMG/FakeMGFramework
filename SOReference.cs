using UnityEngine;
using Object = UnityEngine.Object;

namespace FakeMG.Framework
{
    public abstract class SOReference<T> : ScriptableObject where T : Object
    {
        public T Value { get; private set; }

        public void Set(T newValue)
        {
            if (Value != null)
            {
                Debug.LogWarning($"Cannot set SOReference<{typeof(T).Name}> because it is already assigned.");
                return;
            }

            Value = newValue;
        }

        public bool IsAssigned => Value;
    }
}