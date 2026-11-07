using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FakeMG.Framework.UI
{
    /// <summary>
    /// A list of views reused between refreshes: showing N views activates the first N, creating any that are missing,
    /// and deactivates the rest. Views are never destroyed, so a list that refreshes often allocates nothing once it has
    /// grown to its largest size.
    /// </summary>
    /// <remarks>
    /// Views shown again keep their active state, so a refresh does not run their OnDisable/OnEnable. Bind each shown view
    /// after showing it: a reused view still holds whatever it showed last.
    /// </remarks>
    public sealed class ReusableViewList<TView> where TView : Component
    {
        private readonly TView _prefab;
        private readonly Transform _parent;
        private readonly Func<TView> _createView;
        private readonly List<TView> _views = new();

        /// <summary>How many views are shown, always the first ones.</summary>
        public int ShownCount { get; private set; }

        /// <summary>Every view created so far, shown or not, e.g. to unsubscribe from them on teardown.</summary>
        public IReadOnlyList<TView> CreatedViews => _views;

        /// <summary>A shown view.</summary>
        public TView this[int index]
        {
            get
            {
                if (index < 0 || index >= ShownCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, $"Only {ShownCount} views are shown.");
                }

                return _views[index];
            }
        }

        #region Public Methods

        /// <summary>Creates views by instantiating the prefab under the parent.</summary>
        public ReusableViewList(TView prefab, Transform parent)
        {
            _prefab = prefab;
            _parent = parent;
        }

        /// <summary>Creates views with the given factory, e.g. to inject them or subscribe to their events once.</summary>
        public ReusableViewList(Func<TView> createView)
        {
            _createView = createView;
        }

        /// <summary>Shows exactly the first <paramref name="count"/> views.</summary>
        public void Show(int count)
        {
            for (int i = ShownCount; i < count; i++)
            {
                ShowAt(i);
            }

            HideFrom(count);
        }

        /// <summary>
        /// Shows the view at the index, creating it if needed, for lists whose length is only known while filling them.
        /// Fill in order from index 0, then call <see cref="HideFrom"/> with the number filled.
        /// </summary>
        public TView ShowAt(int index)
        {
            if (index < 0 || index > ShownCount)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, $"Views fill in order; the next one is {ShownCount}.");
            }

            while (_views.Count <= index)
            {
                _views.Add(CreateHiddenView());
            }

            TView view = _views[index];
            view.gameObject.SetActive(true);
            ShownCount = Mathf.Max(ShownCount, index + 1);
            return view;
        }

        /// <summary>Hides every shown view from the index on.</summary>
        public void HideFrom(int count)
        {
            int keptCount = Mathf.Max(0, count);
            for (int i = keptCount; i < ShownCount; i++)
            {
                _views[i].gameObject.SetActive(false);
            }

            ShownCount = Mathf.Min(ShownCount, keptCount);
        }

        #endregion

        #region Private Methods

        private TView CreateHiddenView()
        {
            TView view = _createView != null ? _createView() : Object.Instantiate(_prefab, _parent);
            view.gameObject.SetActive(false);
            return view;
        }

        #endregion
    }
}
