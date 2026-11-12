using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class OffHandGripSolverTests
    {
        private const float BLEND_DURATION_SECONDS = 0.2f;

        private OffHandGripSolver _solver;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _solver = new OffHandGripSolver();
        }

        [Test]
        public void MeasureGrip_ReplaysTheGripOntoAMovedHoldingHand()
        {
            Quaternion measuredHandRotation = Quaternion.Euler(0f, 90f, 0f);
            Vector3 gripPositionMeters = new(1f, 0f, 0.5f);
            Quaternion gripRotation = Quaternion.Euler(0f, 90f, 30f);
            _solver.MeasureGrip(Vector3.zero, measuredHandRotation, gripPositionMeters, gripRotation);

            Vector3 movedHandPositionMeters = new(10f, 0f, 0f);
            Vector3 offHandPositionMeters = _solver.GetOffHandPositionMeters(movedHandPositionMeters, measuredHandRotation);
            Quaternion offHandRotation = _solver.GetOffHandRotation(measuredHandRotation);

            Assert.That(_solver.HasMeasuredGrip, Is.True);
            Assert.That(Vector3.Distance(offHandPositionMeters, movedHandPositionMeters + gripPositionMeters), Is.LessThan(0.0001f));
            Assert.That(Quaternion.Angle(offHandRotation, gripRotation), Is.LessThan(0.01f));
        }

        [Test]
        public void MeasureGrip_FollowsTheHoldingHandRotation()
        {
            _solver.MeasureGrip(Vector3.zero, Quaternion.identity, Vector3.forward, Quaternion.identity);

            Vector3 offHandPositionMeters = _solver.GetOffHandPositionMeters(Vector3.zero, Quaternion.Euler(0f, 90f, 0f));

            Assert.That(Vector3.Distance(offHandPositionMeters, Vector3.right), Is.LessThan(0.0001f));
        }

        [Test]
        public void BlendWeight_WhileGripping_ReachesFullWeightAfterTheBlendDuration()
        {
            _solver.BlendWeight(isGripping: true, BLEND_DURATION_SECONDS * 0.5f, BLEND_DURATION_SECONDS);
            Assert.That(_solver.Weight01, Is.EqualTo(0.5f).Within(0.0001f));

            _solver.BlendWeight(isGripping: true, BLEND_DURATION_SECONDS, BLEND_DURATION_SECONDS);
            Assert.That(_solver.Weight01, Is.EqualTo(1f));
        }

        [Test]
        public void ReleaseGrip_DropsTheWeightAndTheMeasurementAtOnce()
        {
            _solver.BlendWeight(isGripping: true, BLEND_DURATION_SECONDS, BLEND_DURATION_SECONDS);
            _solver.MeasureGrip(Vector3.zero, Quaternion.identity, Vector3.forward, Quaternion.identity);

            _solver.ReleaseGrip();

            Assert.That(_solver.Weight01, Is.Zero);
            Assert.That(_solver.HasMeasuredGrip, Is.False);
        }

        [Test]
        public void ForgetGrip_KeepsTheWeightButRequiresANewMeasurement()
        {
            _solver.BlendWeight(isGripping: true, BLEND_DURATION_SECONDS, BLEND_DURATION_SECONDS);
            _solver.MeasureGrip(Vector3.zero, Quaternion.identity, Vector3.forward, Quaternion.identity);

            _solver.ForgetGrip();

            Assert.That(_solver.Weight01, Is.EqualTo(1f));
            Assert.That(_solver.HasMeasuredGrip, Is.False);
        }

        #endregion
    }
}
