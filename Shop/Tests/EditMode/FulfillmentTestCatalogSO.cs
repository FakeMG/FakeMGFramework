using FakeMG.Shop.Config;

namespace FakeMG.Shop.Tests
{
    public sealed class FulfillmentTestCatalogSO : ShopListingDatabaseSO
    {
        #region Public Methods

        public void AddListing(ShopListingSO listingSO) => _items.Add(listingSO.Id, listingSO);

        #endregion
    }
}
