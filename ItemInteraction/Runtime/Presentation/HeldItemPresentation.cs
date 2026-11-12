using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public sealed class HeldItemPresentation
    {
        private readonly IReadOnlyList<IOffHandGripPresentation> _offHandGrips;

        public HeldItemPresentation(IReadOnlyList<IOffHandGripPresentation> offHandGrips)
        {
            _offHandGrips = offHandGrips;
        }

        #region Public Methods

        public void ApplyHandPoseAndBeginGrips(IHeldItem item, GameObject heldItemInstance)
        {
            HandPose handPose = item.HoldingHandPose;
            heldItemInstance.transform.SetLocalPositionAndRotation(
                handPose.LocalPositionMeters,
                Quaternion.Euler(handPose.LocalRotationDegrees));

            foreach (IOffHandGripPresentation offHandGrip in _offHandGrips)
            {
                offHandGrip.BeginOffHandGrip(item.HoldingHand, heldItemInstance);
            }
        }

        public void ReleaseGrips()
        {
            foreach (IOffHandGripPresentation offHandGrip in _offHandGrips)
            {
                offHandGrip.ReleaseOffHandGrip();
            }
        }

        #endregion
    }
}
