using UnityEngine;

namespace FakeMG.GodCamera
{
    /// <summary>
    /// Calculates profile-limited zoom targets and world-space anchor corrections.
    /// </summary>
    public class CameraZoomCalculator
    {
        #region Public Methods

        public float CalculateTargetZoomMeters(
            float currentTargetZoomMeters,
            float zoomDeltaMeters,
            CameraProfileSO profile)
        {
            // Positive input means zooming in, so it reduces orthographic size or camera distance.
            return Mathf.Clamp(
                currentTargetZoomMeters - zoomDeltaMeters,
                profile.MinimumZoomMeters,
                profile.MaximumZoomMeters);
        }

        public Vector3 CalculateAnchorCorrectionMeters(
            Vector3 originalAnchorMeters,
            Vector3 targetAnchorMeters)
        {
            // Translating focus by the hit-point difference keeps the same world point under the screen anchor.
            Vector3 correctionMeters = originalAnchorMeters - targetAnchorMeters;
            correctionMeters.y = 0f;
            return correctionMeters;
        }

        #endregion
    }
}
