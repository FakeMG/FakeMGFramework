using UnityEngine;

namespace FakeMG.FakeMGFramework {
    /// <summary>
    /// <para>
    /// Generic singleton base class for MonoBehaviour components. Any class that inherits from this becomes a Singleton.
    /// </para>
    /// <para>Singleton objects should be in a separate Manager scene.</para>
    /// </summary>
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour {
        public static T Instance { get; private set; }

        protected virtual void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
        }
    }
}