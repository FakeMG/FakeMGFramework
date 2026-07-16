using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Coalesces procedural-sky changes into bounded environment-cubemap refresh requests.
    /// </summary>
    public sealed class EnvironmentCubemapRefreshController : MonoBehaviour
    {
        private const float ENVIRONMENT_CUBEMAP_REFRESH_INTERVAL_SECONDS = 1f;

        private bool _isRefreshPending;
        private float _lastRefreshTimeSeconds = float.NegativeInfinity;

        #region Unity Lifecycle

        private void Update()
        {
            if (!_isRefreshPending
                || !DynamicGI.isConverged
                || Time.unscaledTime - _lastRefreshTimeSeconds
                < ENVIRONMENT_CUBEMAP_REFRESH_INTERVAL_SECONDS)
            {
                return;
            }

            DynamicGI.UpdateEnvironment();
            _lastRefreshTimeSeconds = Time.unscaledTime;
            _isRefreshPending = false;
        }

        #endregion

        #region Public Methods

        public void RequestRefresh()
        {
            _isRefreshPending = true;
        }

        #endregion
    }
}
