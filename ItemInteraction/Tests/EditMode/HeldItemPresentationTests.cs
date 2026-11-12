using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class HeldItemPresentationTests
    {
        private static readonly Vector3 HAND_POSITION_METERS = new(-0.4f, 0.5f, -0.6f);
        private static readonly Vector3 HAND_ROTATION_DEGREES = new(30f, 0f, 0f);

        private GameObject _heldItemInstance;
        private IOffHandGripPresentation _offHandGrip;
        private HeldItemPresentation _presentation;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _heldItemInstance = new GameObject(nameof(HeldItemPresentationTests));
            _offHandGrip = Substitute.For<IOffHandGripPresentation>();
            _presentation = new HeldItemPresentation(new[] { _offHandGrip });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_heldItemInstance);
        }

        [Test]
        public void ApplyHandPose_PlacesTheInstanceAtTheItemHandPose()
        {
            IHeldItem item = CreateItemHeldIn(HeldItemHand.Left);

            _presentation.ApplyHandPoseAndBeginGrips(item, _heldItemInstance);

            Transform heldItemTransform = _heldItemInstance.transform;
            Assert.That(Vector3.Distance(heldItemTransform.localPosition, HAND_POSITION_METERS), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(heldItemTransform.localRotation, Quaternion.Euler(HAND_ROTATION_DEGREES)), Is.LessThan(0.01f));
        }

        [TestCase(HeldItemHand.Right)]
        [TestCase(HeldItemHand.Left)]
        public void ApplyHandPose_BeginsTheOffHandGripForTheHoldingHand(HeldItemHand holdingHand)
        {
            IHeldItem item = CreateItemHeldIn(holdingHand);

            _presentation.ApplyHandPoseAndBeginGrips(item, _heldItemInstance);

            _offHandGrip.Received(1).BeginOffHandGrip(holdingHand, _heldItemInstance);
        }

        [Test]
        public void ReleaseGrips_ReleasesEveryOffHandGrip()
        {
            _presentation.ReleaseGrips();

            _offHandGrip.Received(1).ReleaseOffHandGrip();
        }

        #endregion

        #region Private Methods

        private static IHeldItem CreateItemHeldIn(HeldItemHand hand)
        {
            var item = Substitute.For<IHeldItem>();
            item.HoldingHand.Returns(hand);
            item.HoldingHandPose.Returns(new HandPose(HAND_POSITION_METERS, HAND_ROTATION_DEGREES));
            return item;
        }

        #endregion
    }
}
