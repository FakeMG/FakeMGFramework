using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Shop.Config;

namespace FakeMG.Shop.UI
{
    internal static class ShopListingPriceTextBuilder
    {
        private const string FREE_PRICE_TEXT = "Free";
        private const string PRICE_SEPARATOR = " + ";

        #region Public Methods

        public static string BuildPriceText(ShopListingViewDefinition shopListingViewDefinition)
        {
            if (!string.IsNullOrWhiteSpace(shopListingViewDefinition.ListingSO.LocalizedIapPriceText))
            {
                return shopListingViewDefinition.ListingSO.LocalizedIapPriceText;
            }

            return BuildItemPriceText(shopListingViewDefinition.ListingSO.GetPrice());
        }

        #endregion

        #region Private Methods

        private static string BuildItemPriceText(IReadOnlyDictionary<IdentitySO, int> priceByItem)
        {
            var parts = new List<string>();
            foreach ((IdentitySO itemSo, int amount) in priceByItem)
            {
                if (!itemSo || amount <= 0)
                {
                    continue;
                }

                string itemName = string.IsNullOrWhiteSpace(itemSo.ItemName) ? itemSo.name : itemSo.ItemName;
                parts.Add($"{amount} {itemName}");
            }

            return parts.Count == 0 ? FREE_PRICE_TEXT : string.Join(PRICE_SEPARATOR, parts);
        }

        #endregion
    }
}
