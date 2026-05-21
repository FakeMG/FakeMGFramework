using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Inventory.Hud
{
    public sealed class ItemCounterRegistry
    {
        private readonly Dictionary<IdentitySO, ItemCounterView> _viewsByIdentity = new();

        #region Public Methods

        public void Register(ItemCounterView view)
        {
            if (!view.IdentitySO)
            {
                Debug.LogError($"{nameof(ItemCounterView)} '{view.name}' cannot register without an {nameof(IdentitySO)}.");
                return;
            }

            if (_viewsByIdentity.TryGetValue(view.IdentitySO, out ItemCounterView existingView) && existingView != view)
            {
                Debug.LogError($"Duplicate HUD counter for item '{view.IdentitySO.name}'. Existing: '{existingView.name}', duplicate: '{view.name}'.");
                return;
            }

            _viewsByIdentity[view.IdentitySO] = view;
        }

        public void Unregister(ItemCounterView view)
        {
            if (!view.IdentitySO)
            {
                Debug.LogError($"{nameof(ItemCounterView)} '{view.name}' cannot unregister because it has no {nameof(IdentitySO)}.");
                return;
            }

            if (_viewsByIdentity.TryGetValue(view.IdentitySO, out ItemCounterView existingView) && existingView == view)
            {
                _viewsByIdentity.Remove(view.IdentitySO);
            }
        }

        public bool TryGetCounter(IdentitySO identity, out ItemCounterView view)
        {
            if (!identity)
            {
                Debug.LogError($"{nameof(ItemCounterRegistry)} cannot resolve a counter for a null item identity.");
                view = null;
                return false;
            }

            return _viewsByIdentity.TryGetValue(identity, out view);
        }

        #endregion
    }
}
