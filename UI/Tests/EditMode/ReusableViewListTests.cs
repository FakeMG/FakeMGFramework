using System;
using System.Collections.Generic;
using FakeMG.Framework.UI;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FakeMG.UI.Tests.EditMode
{
    public sealed class ReusableViewListTests
    {
        private readonly List<GameObject> _createdObjects = new();
        private Transform _parent;
        private ReusableViewList<Transform> _list;
        private int _createdViewCount;

        [SetUp]
        public void SetUp()
        {
            _parent = Track(new GameObject("Parent")).transform;
            _list = new ReusableViewList<Transform>(CreateView);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject createdObject in _createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            _createdObjects.Clear();
            _createdViewCount = 0;
        }

        [Test]
        public void Show_ActivatesThatManyViews()
        {
            _list.Show(3);

            Assert.That(_list.ShownCount, Is.EqualTo(3));
            Assert.That(_list[2].gameObject.activeSelf, Is.True);
        }

        [Test]
        public void ShowingFewer_HidesTheRestWithoutDestroyingThem()
        {
            _list.Show(3);

            _list.Show(1);

            Assert.That(_list.ShownCount, Is.EqualTo(1));
            Assert.That(_list.CreatedViews[1].gameObject.activeSelf, Is.False);
            Assert.That(_list.CreatedViews.Count, Is.EqualTo(3));
        }

        [Test]
        public void ShowingAgain_ReusesViewsInsteadOfCreatingMore()
        {
            _list.Show(3);
            _list.Show(0);

            _list.Show(2);

            Assert.That(_createdViewCount, Is.EqualTo(3));
        }

        [Test]
        public void Show_Zero_HidesEverything()
        {
            _list.Show(2);

            _list.Show(0);

            Assert.That(_list.ShownCount, Is.Zero);
            Assert.That(_list.CreatedViews[0].gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ShowAtThenHideFrom_FillsAListOfUnknownLength()
        {
            _list.Show(4);

            _list.ShowAt(0);
            _list.ShowAt(1);
            _list.HideFrom(2);

            Assert.That(_list.ShownCount, Is.EqualTo(2));
            Assert.That(_list.CreatedViews[3].gameObject.activeSelf, Is.False);
        }

        [Test]
        public void ShowAt_SkippingAnIndex_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _list.ShowAt(1));
        }

        [Test]
        public void Indexer_PastTheShownViews_Throws()
        {
            _list.Show(2);
            _list.Show(1);

            Assert.Throws<ArgumentOutOfRangeException>(() => _ = _list[1]);
        }

        [Test]
        public void PrefabConstructor_InstantiatesUnderTheParent()
        {
            Transform prefab = Track(new GameObject("Prefab")).transform;
            var list = new ReusableViewList<Transform>(prefab, _parent);

            list.Show(1);
            Track(list[0].gameObject);

            Assert.That(list[0].parent, Is.SameAs(_parent));
        }

        private Transform CreateView()
        {
            _createdViewCount++;
            return Track(new GameObject($"View {_createdViewCount}")).transform;
        }

        private GameObject Track(GameObject createdObject)
        {
            _createdObjects.Add(createdObject);
            return createdObject;
        }
    }
}
