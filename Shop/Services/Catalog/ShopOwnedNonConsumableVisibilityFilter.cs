using System.Collections.Generic;
using FakeMG.Shop.Config;

namespace FakeMG.Shop.Services.Catalog
{
    public class ShopOwnedNonConsumableVisibilityFilter
    {
        private readonly ShopOwnershipStateRepository _shopOwnershipStateRepository;

        public ShopOwnedNonConsumableVisibilityFilter(ShopOwnershipStateRepository shopOwnershipStateRepository)
        {
            _shopOwnershipStateRepository = shopOwnershipStateRepository;
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

                bool shouldHideListing = listing.IsNonConsumable && _shopOwnershipStateRepository.IsOwned(listing.Id);
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
