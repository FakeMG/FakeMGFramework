using UnityEngine;

namespace FakeMG.ItemInteraction
{
    /// <summary>
    /// Solves where the hand that is not holding an item must go to grip it, and how strongly.
    /// The item is rigidly attached to the holding hand, so the grip is measured once per frame relative to that hand
    /// and replayed onto the next frame's animated holding hand without lagging a frame behind the animation.
    /// </summary>
    public sealed class OffHandGripSolver
    {
        private Vector3 _gripOffsetFromHoldingHandMeters;
        private Quaternion _gripRotationFromHoldingHand = Quaternion.identity;

        public bool HasMeasuredGrip { get; private set; }
        public float Weight01 { get; private set; }

        #region Public Methods

        public void ForgetGrip()
        {
            HasMeasuredGrip = false;
        }

        public void ReleaseGrip()
        {
            HasMeasuredGrip = false;
            Weight01 = 0f;
        }

        public void BlendWeight(bool isGripping, float deltaSeconds, float blendDurationSeconds)
        {
            Weight01 = Mathf.MoveTowards(Weight01, isGripping ? 1f : 0f, deltaSeconds / blendDurationSeconds);
        }

        public void MeasureGrip(
            Vector3 holdingHandPositionMeters,
            Quaternion holdingHandRotation,
            Vector3 gripPositionMeters,
            Quaternion gripRotation)
        {
            Quaternion toHoldingHandSpace = Quaternion.Inverse(holdingHandRotation);
            _gripOffsetFromHoldingHandMeters = toHoldingHandSpace * (gripPositionMeters - holdingHandPositionMeters);
            _gripRotationFromHoldingHand = toHoldingHandSpace * gripRotation;
            HasMeasuredGrip = true;
        }

        public Vector3 GetOffHandPositionMeters(Vector3 holdingHandPositionMeters, Quaternion holdingHandRotation)
        {
            return holdingHandPositionMeters + holdingHandRotation * _gripOffsetFromHoldingHandMeters;
        }

        public Quaternion GetOffHandRotation(Quaternion holdingHandRotation)
        {
            return holdingHandRotation * _gripRotationFromHoldingHand;
        }

        #endregion
    }
}
