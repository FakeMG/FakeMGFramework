using System.Collections.Generic;
using FakeMG.Shop.Config;

namespace FakeMG.Shop.Filters
{
    public class ShopOwnedNonConsumableVisibilityFilter
    {
        private readonly ShopOwnershipStateSaveable _shopOwnershipStateSaveable;

        public ShopOwnedNonConsumableVisibilityFilter(ShopOwnershipStateSaveable shopOwnershipStateSaveable)
        {
            _shopOwnershipStateSaveable = shopOwnershipStateSaveable;
        }

        #region Public Methods

        public IReadOnlyList<ShopListingSO> Filter(IReadOnlyList<ShopListingSO> sourceListingSOs)
        {
            var visibleListings = new List<ShopListingSO>();
            if (sourceListingSOs == null)
            {
                return visibleListings;
            }

            foreach (var listing in sourceListingSOs)
            {
                if (listing == null)
                {
                    continue;
                }

                bool shouldHideListing = listing.IsNonConsumable && _shopOwnershipStateSaveable.IsOwned(listing.Id);
                if (shouldHideListing)
                {
                    continue;
                }

                visibleListings.Add(listing);
            }

            return visibleListings;
        }

        #endregion
    }
}
