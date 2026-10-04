using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Defines how a scene object changes and restores its visibility for camera obstruction fading.
    /// </summary>
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
