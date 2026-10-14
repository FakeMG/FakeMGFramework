using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FakeMG.Outline.Tests
{
    public sealed class RenderingLayerOutlineSelectionRegistryTests
    {
        private RenderingLayerOutlineSelectionRegistry _registry;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _registry = new RenderingLayerOutlineSelectionRegistry();
        }

        [Test]
        public void EmptyRegistry_HasNoSelections()
        {
            Assert.That(_registry.HasSelection(uint.MaxValue), Is.False);
            Assert.That(_registry.ActiveSelectionCount, Is.Zero);
        }

        [Test]
        public void RegisteredMask_MatchesAnyIntersectingFeatureMask()
        {
            _registry.RegisterSelection(new object(), 0b0110u);

            Assert.That(_registry.HasSelection(0b1010u), Is.True);
            Assert.That(_registry.HasSelection(0b1000u), Is.False);
            Assert.That(_registry.HasSelection(0u), Is.False);
        }

        [Test]
        public void HighestRenderingLayerBit_IsTracked()
        {
            var owner = new object();
            _registry.RegisterSelection(owner, 0x80000000u);

            Assert.That(_registry.HasSelection(0x80000000u), Is.True);
            _registry.UnregisterSelection(owner);
            Assert.That(_registry.HasSelection(uint.MaxValue), Is.False);
        }

        [Test]
        public void RepeatedRegistrationAndCleanup_AreIdempotent()
        {
            var owner = new object();
            _registry.RegisterSelection(owner, 2u);
            _registry.RegisterSelection(owner, 2u);
            Assert.That(_registry.ActiveSelectionCount, Is.EqualTo(1));

            _registry.UnregisterSelection(owner);
            _registry.UnregisterSelection(owner);

            Assert.That(_registry.HasSelection(2u), Is.False);
            Assert.That(_registry.ActiveSelectionCount, Is.Zero);
        }

        [Test]
        public void UnregisteringOneOwner_PreservesOtherOwnersOnTheSameLayer()
        {
            var firstOwner = new object();
            _registry.RegisterSelection(firstOwner, 2u);
            _registry.RegisterSelection(new object(), 2u);

            _registry.UnregisterSelection(firstOwner);

            Assert.That(_registry.HasSelection(2u), Is.True);
            Assert.That(_registry.ActiveSelectionCount, Is.EqualTo(1));
        }

        [Test]
        public void ReplacingOwnerMask_RemovesOnlyItsPreviousBits()
        {
            var firstOwner = new object();
            _registry.RegisterSelection(firstOwner, 0b0110u);
            _registry.RegisterSelection(new object(), 0b0010u);

            _registry.RegisterSelection(firstOwner, 0b1000u);

            Assert.That(_registry.HasSelection(0b0010u), Is.True);
            Assert.That(_registry.HasSelection(0b0100u), Is.False);
            Assert.That(_registry.HasSelection(0b1000u), Is.True);
            Assert.That(_registry.ActiveSelectionCount, Is.EqualTo(2));
        }

        [Test]
        public void ZeroMask_UnregistersItsOwner()
        {
            var owner = new object();
            _registry.RegisterSelection(owner, uint.MaxValue);

            _registry.RegisterSelection(owner, 0u);

            Assert.That(_registry.HasSelection(uint.MaxValue), Is.False);
            Assert.That(_registry.ActiveSelectionCount, Is.Zero);
        }

        [Test]
        public void ClearingSelections_DiscardsPreviousSessionCounts()
        {
            var previousOwner = new object();
            _registry.RegisterSelection(previousOwner, uint.MaxValue);

            _registry.ClearSelections();
            _registry.UnregisterSelection(previousOwner);
            _registry.RegisterSelection(new object(), 4u);

            Assert.That(_registry.HasSelection(2u), Is.False);
            Assert.That(_registry.HasSelection(4u), Is.True);
            Assert.That(_registry.ActiveSelectionCount, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void NullOwner_IsRejectedWithoutChangingSelections(bool isRegistering)
        {
            _registry.RegisterSelection(new object(), 2u);
#if LOGGER_ENABLED
            string operation = isRegistering ? "registration" : "cleanup";
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex($"Outline selection {operation}.*owner is null"));
#endif
            if (isRegistering)
            {
                _registry.RegisterSelection(null, 2u);
            }
            else
            {
                _registry.UnregisterSelection(null);
            }

            Assert.That(_registry.ActiveSelectionCount, Is.EqualTo(1));
            Assert.That(_registry.HasSelection(2u), Is.True);
        }

        #endregion
    }
}
