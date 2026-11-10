using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class UprightSpineRotationSolverTests
    {
        [Test]
        public void ResolveTargetWorldRotation_QuaternionOffsetMatchesAuthoredDegrees()
        {
            var solver = new UprightSpineRotationSolver();
            Quaternion parentWorldRotation = Quaternion.Euler(60f, 35f, 20f);
            Quaternion animatedLocalRotation = Quaternion.Euler(5f, 10f, 0f);
            Vector3 offsetDegrees = new(0f, 45f, 0f);

            Quaternion fromDegrees = solver.ResolveTargetWorldRotation(parentWorldRotation, animatedLocalRotation, offsetDegrees);
            Quaternion fromQuaternion = solver.ResolveTargetWorldRotation(parentWorldRotation, animatedLocalRotation,
                Quaternion.Euler(offsetDegrees));

            Assert.That(Quaternion.Angle(fromDegrees, fromQuaternion), Is.LessThan(0.01f));
        }

        [Test]
        public void ResolveTargetWorldRotation_ParentYawChanges_FollowsParentHeading()
        {
            var solver = new UprightSpineRotationSolver();

            Quaternion targetWorldRotation = solver.ResolveTargetWorldRotation(
                Quaternion.Euler(0f, 90f, 0f),
                Quaternion.identity,
                Vector3.zero);

            Assert.That(Vector3.Angle(targetWorldRotation * Vector3.forward, Vector3.right), Is.LessThan(0.01f));
        }

        [Test]
        public void ResolveTargetWorldRotation_ParentLiesHorizontal_KeepsTargetUpright()
        {
            var solver = new UprightSpineRotationSolver();

            Quaternion targetWorldRotation = solver.ResolveTargetWorldRotation(
                Quaternion.Euler(90f, 35f, 20f),
                Quaternion.identity,
                Vector3.zero);

            Assert.That(Vector3.Angle(targetWorldRotation * Vector3.up, Vector3.up), Is.LessThan(0.01f));
        }
    }
}
