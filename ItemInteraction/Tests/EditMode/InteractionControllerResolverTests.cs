using FakeMG.ItemInteraction;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.ItemInteraction.Tests
{
    public sealed class InteractionControllerResolverTests
    {
        private IInteractionItem _runtimeItem;

        [SetUp]
        public void SetUp()
        {
            _runtimeItem = new RuntimeItem();
        }

        #region Public Methods

        [Test]
        public void TryResolve_WithExactlyOneItemMatch_ReturnsThatController()
        {
            var expectedController = new StubInteractionController(_runtimeItem, false);
            var resolver = new InteractionControllerResolver(new IInteractionController[]
            {
                new StubInteractionController(null, true),
                expectedController,
            });

            bool hasResolved = resolver.TryResolve(_runtimeItem, null, out IInteractionController resolvedController);

            Assert.That(hasResolved, Is.True);
            Assert.That(resolvedController, Is.SameAs(expectedController));
        }

        [Test]
        public void TryResolve_WithExplicitEmptyHandSupport_ReturnsThatController()
        {
            var expectedController = new StubInteractionController(null, true);
            var resolver = new InteractionControllerResolver(new[] { expectedController });

            bool hasResolved = resolver.TryResolve(null, null, out IInteractionController resolvedController);

            Assert.That(hasResolved, Is.True);
            Assert.That(resolvedController, Is.SameAs(expectedController));
        }

        [Test]
        public void TryResolve_WithNoMatch_RejectsGesture()
        {
            var resolver = new InteractionControllerResolver(new IInteractionController[]
            {
                new StubInteractionController(null, true),
            });

            Assert.That(resolver.TryResolve(_runtimeItem, null, out IInteractionController resolvedController), Is.False);
            Assert.That(resolvedController, Is.Null);
        }

        [Test]
        public void TryResolve_WithAmbiguousMatches_RejectsGesture()
        {
            var resolver = new InteractionControllerResolver(new IInteractionController[]
            {
                new StubInteractionController(_runtimeItem, false),
                new StubInteractionController(_runtimeItem, false),
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
            private readonly bool _supportsEmptyHand;

            public bool IsInteractionActive { get; private set; }

            public StubInteractionController(IInteractionItem supportedItem, bool supportsEmptyHand)
            {
                _supportedItem = supportedItem;
                _supportsEmptyHand = supportsEmptyHand;
            }

            public bool Supports(IInteractionItem selectedItem, ItemActionSO actionSO)
            {
                return actionSO == null && (selectedItem == null ? _supportsEmptyHand : selectedItem == _supportedItem);
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
