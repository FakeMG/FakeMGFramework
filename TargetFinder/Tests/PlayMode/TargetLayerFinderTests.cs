using System.Collections;
using FakeMG.FakeMGFramework.TargetFinder;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TargetFinder.PlayMode
{
    public class TargetLayerFinderTests
    {
        private GameObject _finderObject;
        private TargetLayerFinder _finder;
        private GameObject[] _targetObjects;
        private const int TEST_LAYER = 1; // Layer 8 is typically "PostProcessing"
        private float _interval;

        [SetUp]
        public void Setup()
        {
            _finderObject = new GameObject("Finder");
            _finder = _finderObject.AddComponent<TargetLayerFinder>();

            var targetLayerMaskField = typeof(TargetLayerFinder).GetField("targetLayerMask",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            LayerMask layerMask = LayerMask.GetMask(LayerMask.LayerToName(TEST_LAYER));
            targetLayerMaskField.SetValue(_finder, layerMask);

            var maxTargetsField = typeof(TargetLayerFinder).GetField("maxTargets",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            maxTargetsField.SetValue(_finder, 10);

            var findClosestTargetField = typeof(TargetLayerFinder).GetField("findClosestTarget",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            findClosestTargetField.SetValue(_finder, true);

            var radiusField = typeof(FakeMG.FakeMGFramework.TargetFinder.TargetFinder).GetField("radius",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            radiusField.SetValue(_finder, 5f);

            var targetDetectionInterval = typeof(FakeMG.FakeMGFramework.TargetFinder.TargetFinder).GetField("targetDetectionInterval",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            _interval = (float)targetDetectionInterval.GetValue(_finder);
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_finderObject);
            if (_targetObjects != null)
            {
                foreach (var target in _targetObjects)
                {
                    if (target)
                        Object.DestroyImmediate(target);
                }
            }
        }

        [UnityTest]
        public IEnumerator NoTargetsInRange_ReturnsNull()
        {
            // Arrange & Act
            yield return new WaitForSeconds(_interval);

            // Assert
            Assert.IsNull(_finder.Target);
        }

        [UnityTest]
        public IEnumerator SingleTargetInRange_ReturnsTarget()
        {
            // Arrange
            _targetObjects = new GameObject[1];
            _targetObjects[0] = CreateTargetObject(new Vector3(2f, 0f, 0f));

            // Act
            yield return new WaitForSeconds(_interval);

            // Assert
            Assert.AreEqual(_targetObjects[0], _finder.Target);
        }

        [UnityTest]
        public IEnumerator MultipleTargetsInRange_ReturnsClosestTarget()
        {
            // Arrange
            _targetObjects = new GameObject[3];
            _targetObjects[0] = CreateTargetObject(new Vector3(3f, 0f, 0f));
            _targetObjects[1] = CreateTargetObject(new Vector3(1f, 0f, 0f)); // Closest
            _targetObjects[2] = CreateTargetObject(new Vector3(4f, 0f, 0f));

            // Act
            yield return new WaitForSeconds(_interval);

            // Assert
            Assert.AreEqual(_targetObjects[1], _finder.Target);
        }

        [UnityTest]
        public IEnumerator TargetGoesOutOfRange_ReturnsNull()
        {
            // Arrange
            _targetObjects = new GameObject[1];
            _targetObjects[0] = CreateTargetObject(new Vector3(1f, 0f, 0f));

            // Act
            yield return new WaitForSeconds(_interval);
            Assert.AreEqual(_targetObjects[0], _finder.Target);

            _targetObjects[0].transform.position = new Vector3(10f, 0f, 0f);
            yield return new WaitForSeconds(_interval);

            // Assert
            Assert.IsNull(_finder.Target);
        }

        [UnityTest]
        public IEnumerator TargetGoesInRange_ReturnsTarget()
        {
            // Arrange
            _targetObjects = new GameObject[1];
            _targetObjects[0] = CreateTargetObject(new Vector3(10f, 0f, 0f));

            // Act
            yield return new WaitForSeconds(_interval);
            Assert.IsNull(_finder.Target);

            _targetObjects[0].transform.position = new Vector3(1f, 0f, 0f);
            yield return new WaitForSeconds(_interval);

            // Assert
            Assert.AreEqual(_targetObjects[0], _finder.Target);
        }

        private GameObject CreateTargetObject(Vector3 position)
        {
            var obj = new GameObject("Target");
            obj.transform.position = position;
            obj.layer = TEST_LAYER;
            obj.AddComponent<BoxCollider>();
            return obj;
        }
    }
}