using System.Collections.Generic;
using FakeMG.Framework;

namespace FakeMG.Inventory.Hud
{
    public sealed class ItemCounterRegistry
    {
        private readonly Dictionary<IdentitySO, List<ItemCounterView>> _viewsByIdentity = new();

        #region Public Methods

        public void Register(ItemCounterView view)
        {
            if (!view.IdentitySO)
            {
                Echo.Error($"{nameof(ItemCounterView)} '{view.name}' cannot register without an {nameof(IdentitySO)}.");
                return;
            }

            if (!_viewsByIdentity.TryGetValue(view.IdentitySO, out List<ItemCounterView> views))
            {
                views = new List<ItemCounterView>();
                _viewsByIdentity[view.IdentitySO] = views;
            }

            if (!views.Contains(view))
            {
                views.Add(view);
            }
        }

        public void Unregister(ItemCounterView view)
        {
            if (!view.IdentitySO)
            {
                Echo.Error($"{nameof(ItemCounterView)} '{view.name}' cannot unregister because it has no {nameof(IdentitySO)}.");
                return;
            }

            if (_viewsByIdentity.TryGetValue(view.IdentitySO, out List<ItemCounterView> views))
            {
                views.Remove(view);
            }
        }

        public bool TryGetCounter(IdentitySO identitySO, out ItemCounterView view)
        {
            if (TryGetCounters(identitySO, out IReadOnlyList<ItemCounterView> views))
            {
                view = views[0];
                return true;
            }

            view = null;
            return false;
        }

        public bool TryGetCounters(IdentitySO identitySO, out IReadOnlyList<ItemCounterView> views)
        {
            if (!identitySO)
            {
                Echo.Error($"{nameof(ItemCounterRegistry)} cannot resolve counters for a null item identity.");
                views = null;
                return false;
            }

            if (_viewsByIdentity.TryGetValue(identitySO, out List<ItemCounterView> registeredViews) && registeredViews.Count > 0)
            {
                views = registeredViews;
                return true;
            }

            views = null;
            return false;
        }

        #endregion
    }
}
