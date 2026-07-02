using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Inventory;
using FakeMG.Numbers;
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

        private static string BuildItemPriceText(IReadOnlyList<ItemAmountEntry> priceEntries)
        {
            var parts = new List<string>();
            for (int entryIndex = 0; entryIndex < priceEntries.Count; entryIndex++)
            {
                ItemAmountEntry entry = priceEntries[entryIndex];
                if (!entry.IdentitySO || entry.Amount <= GameNumber.Zero)
                {
                    continue;
                }

                IdentitySO itemSo = entry.IdentitySO;
                string itemName = string.IsNullOrWhiteSpace(itemSo.ItemName) ? itemSo.name : itemSo.ItemName;
                parts.Add($"{entry.Amount.SeparateNumberWithComma()} {itemName}");
            }

            return parts.Count == 0 ? FREE_PRICE_TEXT : string.Join(PRICE_SEPARATOR, parts);
        }

        #endregion
    }
}
