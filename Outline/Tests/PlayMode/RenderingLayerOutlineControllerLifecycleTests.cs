#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace FakeMG.Outline.Tests
{
    public sealed class RenderingLayerOutlineControllerLifecycleTests
    {
        private const string PREFAB_GUID = "9a6acdf3cb2efbb4682df85875fa79bf";
        private const uint SELECTION_MASK = 2u;
        private const uint ORIGINAL_RENDERING_MASK = 1u;

        private readonly List<GameObject> _instances = new();
        private IObjectResolver _container;
        private GameObject _prefab;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(PREFAB_GUID));
            Assert.That(_prefab, Is.Not.Null);
            _container = new ContainerBuilder().Build();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject instance in _instances)
            {
                if (instance != null)
                {
                    Object.DestroyImmediate(instance);
                }
            }

            _instances.Clear();
            _container.Dispose();
        }

        [Test]
        public void Selection_RegistersTheMaskAndPreservesOtherRenderingLayers()
        {
            RenderingLayerOutlineController controller = InstantiateController();

            controller.SetSelected(true);

            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.True);
            Assert.That(controller.GetComponent<Renderer>().renderingLayerMask, Is.EqualTo(ORIGINAL_RENDERING_MASK | SELECTION_MASK));
        }

        [Test]
        public void DeselectingOneController_PreservesTheOtherSelection()
        {
            RenderingLayerOutlineController first = InstantiateController();
            RenderingLayerOutlineController second = InstantiateController();
            first.SetSelected(true);
            second.SetSelected(true);

            first.SetSelected(false);

            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.True);
            Assert.That(first.GetComponent<Renderer>().renderingLayerMask, Is.EqualTo(ORIGINAL_RENDERING_MASK));
            Assert.That(second.GetComponent<Renderer>().renderingLayerMask, Is.EqualTo(ORIGINAL_RENDERING_MASK | SELECTION_MASK));
        }

        [Test]
        public void RepeatedSelection_DoesNotDuplicateRegistration()
        {
            int initialSelectionCount = RenderingLayerOutlineSelectionRegistry.Shared.ActiveSelectionCount;
            RenderingLayerOutlineController controller = InstantiateController();

            controller.SetSelected(true);
            controller.SetSelected(true);

            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.ActiveSelectionCount, Is.EqualTo(initialSelectionCount + 1));
            controller.SetSelected(false);
            controller.SetSelected(false);
            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.ActiveSelectionCount, Is.EqualTo(initialSelectionCount));
        }

        [Test]
        public void DisableAndReuse_RestoreThenRegisterTheSelection()
        {
            RenderingLayerOutlineController controller = InstantiateController();
            controller.SetSelected(true);

            controller.transform.parent.gameObject.SetActive(false);

            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.False);
            Assert.That(controller.GetComponent<Renderer>().renderingLayerMask, Is.EqualTo(ORIGINAL_RENDERING_MASK));
            controller.transform.parent.gameObject.SetActive(true);
            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.False);
            controller.SetSelected(true);
            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.True);
        }

        [Test]
        public void DestroyingASelectedController_UnregistersItsSelection()
        {
            RenderingLayerOutlineController controller = InstantiateController();
            controller.SetSelected(true);

            Object.DestroyImmediate(controller.transform.parent.gameObject);

            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.False);
        }

        [Test]
        public void DestroyAfterDisable_DoesNotRemoveAnotherOwnersSelection()
        {
            RenderingLayerOutlineController first = InstantiateController();
            RenderingLayerOutlineController second = InstantiateController();
            first.SetSelected(true);
            second.SetSelected(true);

            first.transform.parent.gameObject.SetActive(false);
            Object.DestroyImmediate(first.transform.parent.gameObject);

            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.True);
            second.SetSelected(false);
            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.False);
        }

        [Test]
        public void SelectingWhileDisabled_RegistersOnlyAfterEnable()
        {
            RenderingLayerOutlineController controller = InstantiateController();
            controller.transform.parent.gameObject.SetActive(false);

            controller.SetSelected(true);

            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.False);
            controller.transform.parent.gameObject.SetActive(true);
            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.True);
        }

        [Test]
        public void ReassertingSelectionAfterRegistryReset_RestoresOneRegistration()
        {
            RenderingLayerOutlineController controller = InstantiateController();
            controller.SetSelected(true);
            RenderingLayerOutlineSelectionRegistry.Shared.ClearSelections();

            controller.SetSelected(true);
            controller.SetSelected(true);

            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.HasSelection(SELECTION_MASK), Is.True);
            Assert.That(RenderingLayerOutlineSelectionRegistry.Shared.ActiveSelectionCount, Is.EqualTo(1));
        }

        #endregion

        #region Private Methods

        private RenderingLayerOutlineController InstantiateController()
        {
            GameObject instance = _container.Instantiate(_prefab);
            _instances.Add(instance);
            return instance.GetComponentInChildren<RenderingLayerOutlineController>();
        }

        #endregion
    }
}
#endif
