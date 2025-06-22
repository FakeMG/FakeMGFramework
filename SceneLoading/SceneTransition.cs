using UnityEngine;

namespace FakeMG.FakeMGFramework.SceneLoading {
    public abstract class SceneTransition : MonoBehaviour {
        public abstract void Show();
        public abstract void Hide();
    }
}