using System.Collections;
using DG.Tweening;
using FakeMG.Framework.ExtensionMethods;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace FakeMG.ExtensionMethods.Tests
{
    public sealed class ScrollRectExtensionsLifetimeTests
    {
        private const float VIEWPORT_SIZE_PIXELS = 100f;
        private const float CONTENT_SIZE_PIXELS = 400f;

        private GameObject _root;

        #region Public Methods

        [TearDown]
        public void TearDown()
        {
            if (_root)
            {
                Object.Destroy(_root);
            }
        }

        [UnityTest]
        public IEnumerator ScrollTo_WhenTheScrollRectIsDestroyedMidScroll_EndsTheTween([Values] bool isVertical)
        {
            ScrollRect scrollRect = CreateScrollRect(out Transform target);
            scrollRect.ScrollTo(target, isVertical);
            Assert.That(DOTween.IsTweening(scrollRect), Is.True);

            Object.Destroy(_root);
            yield return null;
            yield return null;

            Assert.That(DOTween.IsTweening(scrollRect), Is.False);
        }

        #endregion

        #region Private Methods

        private ScrollRect CreateScrollRect(out Transform target)
        {
            _root = new GameObject(nameof(ScrollRectExtensionsLifetimeTests), typeof(RectTransform));
            ((RectTransform)_root.transform).sizeDelta = Vector2.one * VIEWPORT_SIZE_PIXELS;
            var content = new GameObject("Content", typeof(RectTransform)).GetComponent<RectTransform>();
            content.SetParent(_root.transform, false);
            content.sizeDelta = Vector2.one * CONTENT_SIZE_PIXELS;
            target = new GameObject("Target", typeof(RectTransform)).transform;
            target.SetParent(content, false);

            var scrollRect = _root.AddComponent<ScrollRect>();
            scrollRect.content = content;
            return scrollRect;
        }

        #endregion
    }
}
