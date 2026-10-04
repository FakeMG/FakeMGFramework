using System;
using FakeMG.Shop.Config;

namespace FakeMG.Shop.UI
{
    public interface IShopListingCardView
    {
        event Action<ShopListingSO> BuyPressed;

        string ListingId { get; }

        void BindListing(ShopListingViewDefinition shopListingViewDefinition);
        void SetVisible(bool isVisible);
    }
}
