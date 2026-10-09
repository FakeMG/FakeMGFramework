using FakeMG.Framework.UI;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.Framework.Shared.Tests
{
    public sealed class VirtualTrackballTests
    {
        #region Public Methods

        [TestCase(0f, 0f, 0.5f, 0f, 1)]
        [TestCase(0f, 0f, 0f, 0.5f, 0)]
        [TestCase(1f, 0f, 0f, 1f, 2)]
        public void CalculateRotation_RotatesAroundAllAxes(float fromX01, float fromY01, float toX01, float toY01, int axisIndex)
        {
            Quaternion rotation = VirtualTrackball.CalculateRotation(new Vector2(fromX01, fromY01), new Vector2(toX01, toY01));

            rotation.ToAngleAxis(out float rotationDegrees, out Vector3 axis);
            Assert.That(rotationDegrees, Is.GreaterThan(0f));
            Assert.That(Mathf.Abs(axis[axisIndex]), Is.GreaterThan(0.99f));
        }

        [Test]
        public void CalculateRotation_StationaryPointerReturnsIdentity()
        {
            Quaternion rotation = VirtualTrackball.CalculateRotation(Vector2.zero, Vector2.zero);

            Assert.That(rotation, Is.EqualTo(Quaternion.identity));
        }

        [Test]
        public void CalculateRotation_OutsidePointersClampToSphereRim()
        {
            Quaternion rotation = VirtualTrackball.CalculateRotation(new Vector2(100f, 0f), new Vector2(0f, 100f));

            Assert.That(Quaternion.Angle(rotation, Quaternion.AngleAxis(90f, Vector3.forward)), Is.LessThan(0.01f));
        }

        #endregion
    }
}
