using UnityEngine;

namespace FakeMG.GodCamera
{
    public abstract class FadeableObject : MonoBehaviour
    {
        #region Public Methods

        public abstract bool UpdateFade(
            float targetAlpha01,
            float fadeDurationSeconds,
            float deltaTimeSeconds);

        public abstract void Release();

        #endregion
    }
}
