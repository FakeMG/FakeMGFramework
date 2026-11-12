using FakeMG.ItemInteraction;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class InteractionControllerResolverTests
    {
        private IInteractionItem _runtimeItem;
        private EmptyHandItemSO _emptyHandItemSO;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _runtimeItem = new RuntimeItem();
            _emptyHandItemSO = ScriptableObject.CreateInstance<EmptyHandItemSO>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_emptyHandItemSO);
        }

        [Test]
        public void TryResolve_WithExactlyOneItemMatch_ReturnsThatController()
        {
            var expectedController = new StubInteractionController(_runtimeItem);
            var resolver = new InteractionControllerResolver(new IInteractionController[]
            {
                new StubInteractionController(_emptyHandItemSO),
                expectedController,
            });

            bool hasResolved = resolver.TryResolve(_runtimeItem, null, out IInteractionController resolvedController);

            Assert.That(hasResolved, Is.True);
            Assert.That(resolvedController, Is.SameAs(expectedController));
        }

        [Test]
        public void TryResolve_WithTheEmptyHandItem_ReturnsTheControllerThatSupportsIt()
        {
            var expectedController = new StubInteractionController(_emptyHandItemSO);
            var resolver = new InteractionControllerResolver(new[] { expectedController });

            bool hasResolved = resolver.TryResolve(_emptyHandItemSO, null, out IInteractionController resolvedController);

            Assert.That(hasResolved, Is.True);
            Assert.That(resolvedController, Is.SameAs(expectedController));
        }

        [Test]
        public void TryResolve_WithNoMatch_RejectsGesture()
        {
            var resolver = new InteractionControllerResolver(new IInteractionController[]
            {
                new StubInteractionController(_emptyHandItemSO),
            });

            Assert.That(resolver.TryResolve(_runtimeItem, null, out IInteractionController resolvedController), Is.False);
            Assert.That(resolvedController, Is.Null);
        }

        [Test]
        public void TryResolve_WithAmbiguousMatches_RejectsGesture()
        {
            var resolver = new InteractionControllerResolver(new IInteractionController[]
            {
                new StubInteractionController(_runtimeItem),
                new StubInteractionController(_runtimeItem),
            });

            Assert.That(resolver.TryResolve(_runtimeItem, null, out IInteractionController resolvedController), Is.False);
            Assert.That(resolvedController, Is.Null);
        }

        #endregion

        private sealed class RuntimeItem : IInteractionItem
        {
            public string DisplayName => "Runtime tool";
        }

        private sealed class StubInteractionController : IInteractionController
        {
            private readonly IInteractionItem _supportedItem;

            public bool IsInteractionActive { get; private set; }

            public StubInteractionController(IInteractionItem supportedItem)
            {
                _supportedItem = supportedItem;
            }

            public bool Supports(IInteractionItem selectedItem, ItemActionSO actionSO)
            {
                return actionSO == null && selectedItem == _supportedItem;
            }

            public bool TryBeginInteraction(IInteractionItem selectedItem, ItemActionSO actionSO, Ray targetRay,
                Vector3 interactionOriginPositionMeters, Vector2 screenPositionPixels)
            {
                IsInteractionActive = true;
                return true;
            }
            public void MovePointer(Ray targetRay, Vector2 screenPositionPixels) { }
            public void Tick(float deltaSeconds) { }
            public void ReleaseInteraction() { IsInteractionActive = false; }
            public void CancelInteraction() { IsInteractionActive = false; }
        }
    }
}
