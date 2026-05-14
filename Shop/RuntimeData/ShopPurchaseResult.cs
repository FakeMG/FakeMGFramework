using System.Collections.Generic;
using FakeMG.Framework;

namespace FakeMG.Shop.RuntimeData
{
    public class ShopPurchaseResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public IReadOnlyDictionary<IdentitySO, int> GrantedItemsByItem { get; }

        private ShopPurchaseResult(bool isSuccess, string message, IReadOnlyDictionary<IdentitySO, int> grantedItemsByItem)
        {
            IsSuccess = isSuccess;
            Message = message;
            GrantedItemsByItem = grantedItemsByItem;
        }

        #region Public Methods

        public static ShopPurchaseResult Succeeded(IReadOnlyDictionary<IdentitySO, int> grantedItemsByItem)
        {
            return new ShopPurchaseResult(true, string.Empty, grantedItemsByItem);
        }

        public static ShopPurchaseResult Failed(string message)
        {
            return new ShopPurchaseResult(false, message, new Dictionary<IdentitySO, int>());
        }

        #endregion
    }
}
