using System.Collections.Generic;
using FakeMG.Inventory;

namespace FakeMG.Shop.RuntimeData
{
    public class ShopPurchaseResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public IReadOnlyList<ItemAmountEntry> GrantedItems { get; }

        private ShopPurchaseResult(bool isSuccess, string message, IReadOnlyList<ItemAmountEntry> grantedItems)
        {
            IsSuccess = isSuccess;
            Message = message;
            GrantedItems = grantedItems;
        }

        #region Public Methods

        public static ShopPurchaseResult Succeeded(IReadOnlyList<ItemAmountEntry> grantedItems)
        {
            return new ShopPurchaseResult(true, string.Empty, grantedItems);
        }

        public static ShopPurchaseResult Failed(string message)
        {
            return new ShopPurchaseResult(false, message, new List<ItemAmountEntry>());
        }

        #endregion
    }
}
