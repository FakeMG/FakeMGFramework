using UnityEngine;
using Object = UnityEngine.Object;

namespace FakeMG.Framework
{
    public abstract class SOReference<T> : ScriptableObject where T : Object
    {
        public T Value { get; private set; }

        public void Set(T newValue) => Value = newValue;
        public bool IsAssigned => Value;
    }
}