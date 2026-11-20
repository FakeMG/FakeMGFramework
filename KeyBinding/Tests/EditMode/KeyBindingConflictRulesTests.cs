using System.Collections.Generic;
using NUnit.Framework;

namespace FakeMG.KeyBinding.Tests
{
    public class KeyBindingConflictRulesTests
    {
        private const string CAMERA_MAP = "Camera";
        private const string TIME_MAP = "Time Controller";
        private const string BUILD_MENU_MAP = "Placement Menu";

        private KeyBindingConflictRules _rules;

        [SetUp]
        public void SetUp()
        {
            _rules = new KeyBindingConflictRules(new[] { (CAMERA_MAP, BUILD_MENU_MAP) });
        }

        [Test]
        public void FindConflictingSlot_SameKeyInMapsLiveTogether_ReturnsOtherSlot()
        {
            var changedSlot = new FakeBoundKey(CAMERA_MAP, "<Keyboard>/space");
            var otherSlot = new FakeBoundKey(TIME_MAP, "<Keyboard>/space");

            FakeBoundKey conflictingSlot = _rules.FindConflictingSlot(new[] { changedSlot, otherSlot }, changedSlot);

            Assert.That(conflictingSlot, Is.SameAs(otherSlot));
        }

        [Test]
        public void FindConflictingSlot_SameKeyInSameMap_ReturnsOtherSlot()
        {
            var changedSlot = new FakeBoundKey(CAMERA_MAP, "<Keyboard>/s");
            var otherSlot = new FakeBoundKey(CAMERA_MAP, "<Keyboard>/s");

            FakeBoundKey conflictingSlot = _rules.FindConflictingSlot(new[] { changedSlot, otherSlot }, changedSlot);

            Assert.That(conflictingSlot, Is.SameAs(otherSlot));
        }

        [Test]
        public void FindConflictingSlot_SameKeyInMapsNeverLiveTogether_ReturnsNull()
        {
            var changedSlot = new FakeBoundKey(CAMERA_MAP, "<Keyboard>/q");
            var otherSlot = new FakeBoundKey(BUILD_MENU_MAP, "<Keyboard>/q");

            FakeBoundKey conflictingSlot = _rules.FindConflictingSlot(new[] { changedSlot, otherSlot }, changedSlot);

            Assert.That(conflictingSlot, Is.Null);
        }

        [Test]
        public void FindConflictingSlot_MapPairListedInEitherOrder_ReturnsNull()
        {
            var changedSlot = new FakeBoundKey(BUILD_MENU_MAP, "<Keyboard>/q");
            var otherSlot = new FakeBoundKey(CAMERA_MAP, "<Keyboard>/q");

            FakeBoundKey conflictingSlot = _rules.FindConflictingSlot(new[] { changedSlot, otherSlot }, changedSlot);

            Assert.That(conflictingSlot, Is.Null);
        }

        [Test]
        public void FindConflictingSlot_DifferentCase_ReturnsOtherSlot()
        {
            var changedSlot = new FakeBoundKey(TIME_MAP, "<keyboard>/space");
            var otherSlot = new FakeBoundKey(TIME_MAP, "<Keyboard>/Space");

            FakeBoundKey conflictingSlot = _rules.FindConflictingSlot(new[] { changedSlot, otherSlot }, changedSlot);

            Assert.That(conflictingSlot, Is.SameAs(otherSlot));
        }

        [Test]
        public void FindConflictingSlot_OnlyTheChangedSlotHoldsTheKey_ReturnsNull()
        {
            var changedSlot = new FakeBoundKey(TIME_MAP, "<Keyboard>/space");
            var otherSlot = new FakeBoundKey(TIME_MAP, "<Keyboard>/1");

            FakeBoundKey conflictingSlot = _rules.FindConflictingSlot(new[] { changedSlot, otherSlot }, changedSlot);

            Assert.That(conflictingSlot, Is.Null);
        }

        [Test]
        public void FindConflictingSlot_ChangedSlotUnbound_ReturnsNull()
        {
            var changedSlot = new FakeBoundKey(TIME_MAP, string.Empty);
            var otherSlot = new FakeBoundKey(TIME_MAP, string.Empty);

            FakeBoundKey conflictingSlot = _rules.FindConflictingSlot(new[] { changedSlot, otherSlot }, changedSlot);

            Assert.That(conflictingSlot, Is.Null);
        }

        [Test]
        public void FindConflictingSlot_SeveralConflicts_ReturnsFirst()
        {
            var changedSlot = new FakeBoundKey(TIME_MAP, "<Keyboard>/f");
            var firstOtherSlot = new FakeBoundKey(CAMERA_MAP, "<Keyboard>/f");
            var secondOtherSlot = new FakeBoundKey(TIME_MAP, "<Keyboard>/f");

            FakeBoundKey conflictingSlot = _rules.FindConflictingSlot(new[] { changedSlot, firstOtherSlot, secondOtherSlot }, changedSlot);

            Assert.That(conflictingSlot, Is.SameAs(firstOtherSlot));
        }

        [Test]
        public void FindConflictingSlot_EmptyList_ReturnsNull()
        {
            var changedSlot = new FakeBoundKey(TIME_MAP, "<Keyboard>/f");

            FakeBoundKey conflictingSlot = _rules.FindConflictingSlot(new List<FakeBoundKey>(), changedSlot);

            Assert.That(conflictingSlot, Is.Null);
        }

        private sealed class FakeBoundKey : IBoundKey
        {
            public string ActionMapName { get; }
            public string ControlPath { get; }

            public FakeBoundKey(string actionMapName, string controlPath)
            {
                ActionMapName = actionMapName;
                ControlPath = controlPath;
            }
        }
    }
}
