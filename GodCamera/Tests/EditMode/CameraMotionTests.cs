using NUnit.Framework;
using UnityEngine;

namespace FakeMG.GodCamera.Tests
{
    public sealed class CameraMotionTests
    {
        #region Public Methods

        [TestCase(CameraProjectionType.Orthographic, 6f)]
        [TestCase(CameraProjectionType.Perspective, 12f)]
        public void MotionStateUsesTheConfiguredProjectionWithoutAnEngineObject(CameraProjectionType projectionType, float zoomMeters)
        {
            CameraMotionState state = CreateState(projectionType);
            Assert.That(state.TargetZoomMeters, Is.EqualTo(zoomMeters));
            state.SetCurrentZoomMeters(projectionType, 4f);
            Assert.That(state.GetCurrentZoomMeters(projectionType), Is.EqualTo(4f));
            Assert.That(state.TargetZoomMeters, Is.EqualTo(zoomMeters));
        }

        [Test]
        public void DirectionalPanMovesCurrentAndTargetTogetherUsingProvidedInput()
        {
            CameraMotionState state = CreateState();
            var pan = new CameraPanMotion(new CameraPanCalculator());
            pan.ApplyDirectionalPan(state, Vector2.up, 10f, 0f, 0.1f);
            Assert.That(state.CurrentFocusPositionMeters.z, Is.GreaterThan(0f));
            Assert.That(state.TargetFocusPositionMeters, Is.EqualTo(state.CurrentFocusPositionMeters));
        }

        [Test]
        public void InactivePointerDragDoesNotMoveTheCamera()
        {
            CameraMotionState state = CreateState();
            var pan = new CameraPanMotion(new CameraPanCalculator());
            pan.ApplyPointerDragPan(state, Vector3.one, 0f, 0.1f);
            Assert.That(state.CurrentFocusPositionMeters, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void PointerDragRetainsGrabbedGroundPointAndStopsAfterRelease()
        {
            CameraMotionState state = CreateState();
            var pan = new CameraPanMotion(new CameraPanCalculator());
            pan.StartPointerDragPan(state, new Vector3(2f, 0f, 0f));
            pan.ApplyPointerDragPan(state, new Vector3(1f, 0f, 0f), 0f, 0.1f);
            Assert.That(state.CurrentFocusPositionMeters.x, Is.GreaterThan(0f));
            Vector3 stoppedPositionMeters = state.CurrentFocusPositionMeters;
            pan.StopPointerDragPan();
            pan.ApplyPointerDragPan(state, Vector3.zero, 0f, 0.1f);
            Assert.That(pan.IsPointerDragPanActive, Is.False);
            Assert.That(state.CurrentFocusPositionMeters, Is.EqualTo(stoppedPositionMeters));
        }

        [TestCase(100f, 3f)]
        [TestCase(-100f, 10f)]
        [TestCase(0f, 6f)]
        public void ZoomTargetClampsToProvidedLimits(float deltaMeters, float expectedZoomMeters)
        {
            var zoom = new CameraZoomMotion(new CameraZoomCalculator());
            Assert.That(zoom.CalculateTargetZoomMeters(CreateState(), deltaMeters, 3f, 10f), Is.EqualTo(expectedZoomMeters));
        }

        [Test]
        public void ZoomAnchorCorrectionChangesTargetAndSmoothsWithoutAView()
        {
            CameraMotionState state = CreateState();
            var zoom = new CameraZoomMotion(new CameraZoomCalculator());
            zoom.SetZoomTarget(state, 4f, new Vector3(2f, 0f, 0f));
            Assert.That(state.TargetFocusPositionMeters.x, Is.EqualTo(2f));
            zoom.SmoothCurrentZoomState(state, CameraProjectionType.Orthographic, 0f, 0.1f);
            Assert.That(state.CurrentFocusPositionMeters.x, Is.GreaterThan(0f));
            Assert.That(state.GetCurrentZoomMeters(CameraProjectionType.Orthographic), Is.LessThan(6f));
            zoom.StopFocusSmoothing(state);
            Assert.That(state.TargetFocusPositionMeters, Is.EqualTo(state.CurrentFocusPositionMeters));
        }

        #endregion

        #region Private Methods

        private static CameraMotionState CreateState(CameraProjectionType projectionType = CameraProjectionType.Orthographic)
        {
            return new CameraMotionState(new CameraRigState(Vector3.zero, 0f, 6f, 12f), projectionType);
        }

        #endregion
    }
}
