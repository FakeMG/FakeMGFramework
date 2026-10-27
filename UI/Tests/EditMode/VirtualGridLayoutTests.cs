using System;
using FakeMG.Framework.UI;
using NUnit.Framework;
using UnityEngine;

namespace FakeMG.Framework.Tests
{
    public sealed class VirtualGridLayoutTests
    {
        private VirtualGridLayout _layout;

        #region Public Methods

        [SetUp]
        public void SetUp()
        {
            _layout = new VirtualGridLayout(4, new Vector2(186f, 190f), new Vector2(50f, 32f), new Vector2(7f, 10f), 10f);
        }

        [TearDown]
        public void TearDown()
        {
            _layout = null;
        }

        [Test]
        public void ContentHeight_IncludesPartialLastRowAndPadding()
        {
            Assert.That(_layout.GetContentHeightPixels(239), Is.EqualTo(13308f));
        }

        [Test]
        public void EmptyGrid_HasNoVisibleCards()
        {
            Assert.That(_layout.GetVisibleRange(0, 0f, 600f).Count, Is.Zero);
            Assert.That(_layout.GetRequiredCardCount(0, 600f), Is.Zero);
        }

        [Test]
        public void SmallList_UsesOnlyItsOwnCards()
        {
            Assert.That(_layout.GetRequiredCardCount(3, 600f), Is.EqualTo(3));
            Assert.That(_layout.GetVisibleRange(3, 0f, 600f).EndIndexExclusive, Is.EqualTo(3));
        }

        [TestCase(-500f, 0)]
        [TestCase(0f, 0)]
        [TestCase(110000f, 224)]
        public void ElasticOverscroll_KeepsTheVisibleRangeInsideTheList(float scrollOffsetPixels, int expectedFirstIndex)
        {
            VirtualGridRange range = _layout.GetVisibleRange(239, scrollOffsetPixels, 600f);

            Assert.That(range.FirstIndex, Is.EqualTo(expectedFirstIndex));
            Assert.That(range.EndIndexExclusive, Is.LessThanOrEqualTo(239));
            Assert.That(range.Count, Is.LessThanOrEqualTo(_layout.GetRequiredCardCount(239, 600f)));
        }

        [Test]
        public void CardPool_RemainsBoundedWhenCatalogGrows()
        {
            Assert.That(_layout.GetRequiredCardCount(239, 600f), Is.EqualTo(24));
            Assert.That(_layout.GetRequiredCardCount(10000, 600f), Is.EqualTo(24));
        }

        [Test]
        public void ItemPosition_PreservesColumnSpacingAndTopPadding()
        {
            Assert.That(_layout.GetItemPositionPixels(5), Is.EqualTo(new Vector2(243f, -232f)));
        }

        [Test]
        public void InvalidGeometry_IsRejectedDuringInitialization()
        {
            Assert.Throws<ArgumentOutOfRangeException>(CreateInvalidGrid);
        }

        #endregion

        #region Private Methods

        private void CreateInvalidGrid()
        {
            _ = new VirtualGridLayout(0, Vector2.one, Vector2.zero, Vector2.zero, 0f);
        }

        #endregion
    }
}
