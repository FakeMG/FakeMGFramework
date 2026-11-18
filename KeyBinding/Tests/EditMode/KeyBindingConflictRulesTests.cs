using System;
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
        public void FindConflictingSlotIndex_SameKeyInMapsLiveTogether_ReturnsOtherSlot()
        {
            List<BoundKey> slots = new()
            {
                new BoundKey(CAMERA_MAP, "<Keyboard>/q"),
                new BoundKey(TIME_MAP, "<Keyboard>/space")
            };

            int conflictIndex = _rules.FindConflictingSlotIndex(slots, 0, "<Keyboard>/space");

            Assert.AreEqual(1, conflictIndex);
        }

        [Test]
        public void FindConflictingSlotIndex_SameKeyInSameMap_ReturnsOtherSlot()
        {
            List<BoundKey> slots = new()
            {
                new BoundKey(CAMERA_MAP, "<Keyboard>/w"),
                new BoundKey(CAMERA_MAP, "<Keyboard>/s")
            };

            int conflictIndex = _rules.FindConflictingSlotIndex(slots, 0, "<Keyboard>/s");

            Assert.AreEqual(1, conflictIndex);
        }

        [Test]
        public void FindConflictingSlotIndex_SameKeyInExclusiveMaps_ReturnsNoConflict()
        {
            List<BoundKey> slots = new()
            {
                new BoundKey(CAMERA_MAP, "<Keyboard>/e"),
                new BoundKey(BUILD_MENU_MAP, "<Keyboard>/q")
            };

            int conflictIndex = _rules.FindConflictingSlotIndex(slots, 0, "<Keyboard>/q");

            Assert.AreEqual(KeyBindingConflictRules.NO_CONFLICT, conflictIndex);
        }

        [Test]
        public void FindConflictingSlotIndex_ExclusivePairListedInEitherOrder_ReturnsNoConflict()
        {
            List<BoundKey> slots = new()
            {
                new BoundKey(BUILD_MENU_MAP, "<Keyboard>/r"),
                new BoundKey(CAMERA_MAP, "<Keyboard>/q")
            };

            int conflictIndex = _rules.FindConflictingSlotIndex(slots, 0, "<Keyboard>/q");

            Assert.AreEqual(KeyBindingConflictRules.NO_CONFLICT, conflictIndex);
        }

        [Test]
        public void FindConflictingSlotIndex_DifferentCase_ReturnsOtherSlot()
        {
            List<BoundKey> slots = new()
            {
                new BoundKey(TIME_MAP, "<Keyboard>/1"),
                new BoundKey(TIME_MAP, "<Keyboard>/Space")
            };

            int conflictIndex = _rules.FindConflictingSlotIndex(slots, 0, "<keyboard>/space");

            Assert.AreEqual(1, conflictIndex);
        }

        [Test]
        public void FindConflictingSlotIndex_ReboundSlotAlreadyHoldsKey_IgnoresItself()
        {
            List<BoundKey> slots = new()
            {
                new BoundKey(TIME_MAP, "<Keyboard>/space"),
                new BoundKey(TIME_MAP, "<Keyboard>/1")
            };

            int conflictIndex = _rules.FindConflictingSlotIndex(slots, 0, "<Keyboard>/space");

            Assert.AreEqual(KeyBindingConflictRules.NO_CONFLICT, conflictIndex);
        }

        [Test]
        public void FindConflictingSlotIndex_EmptyNewPath_ReturnsNoConflict()
        {
            List<BoundKey> slots = new()
            {
                new BoundKey(TIME_MAP, "<Keyboard>/1"),
                new BoundKey(TIME_MAP, string.Empty)
            };

            int conflictIndex = _rules.FindConflictingSlotIndex(slots, 0, string.Empty);

            Assert.AreEqual(KeyBindingConflictRules.NO_CONFLICT, conflictIndex);
        }

        [Test]
        public void FindConflictingSlotIndex_SeveralClashes_ReturnsFirst()
        {
            List<BoundKey> slots = new()
            {
                new BoundKey(TIME_MAP, "<Keyboard>/1"),
                new BoundKey(CAMERA_MAP, "<Keyboard>/f"),
                new BoundKey(TIME_MAP, "<Keyboard>/f")
            };

            int conflictIndex = _rules.FindConflictingSlotIndex(slots, 0, "<Keyboard>/f");

            Assert.AreEqual(1, conflictIndex);
        }

        [Test]
        public void FindConflictingSlotIndex_ReboundIndexOutOfRange_Throws()
        {
            List<BoundKey> slots = new() { new BoundKey(TIME_MAP, "<Keyboard>/1") };

            Assert.Throws<ArgumentOutOfRangeException>(() => _rules.FindConflictingSlotIndex(slots, 3, "<Keyboard>/2"));
        }
    }
}
