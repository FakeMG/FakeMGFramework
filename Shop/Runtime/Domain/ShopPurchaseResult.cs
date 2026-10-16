using System.Collections.Generic;
using FakeMG.Inventory;

namespace FakeMG.Shop.RuntimeData
{
    public class ShopPurchaseResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public IReadOnlyList<ItemAmountEntry> GrantedItems { get; }
        public string TransactionId { get; }
        public bool IsFinalized { get; }
        public bool IsPaymentPending { get; }

        private ShopPurchaseResult(bool isSuccess, string message, IReadOnlyList<ItemAmountEntry> grantedItems,
            string transactionId = "", bool isFinalized = false, bool isPaymentPending = false)
        {
            IsSuccess = isSuccess;
            Message = message;
            GrantedItems = grantedItems;
            TransactionId = transactionId;
            IsFinalized = isFinalized;
            IsPaymentPending = isPaymentPending;
        }

        #region Public Methods

        public static ShopPurchaseResult Succeeded(IReadOnlyList<ItemAmountEntry> grantedItems, string transactionId = "",
            bool isFinalized = false, bool isPaymentPending = false)
        {
            return new ShopPurchaseResult(true, string.Empty, grantedItems, transactionId, isFinalized, isPaymentPending);
        }

        public static ShopPurchaseResult Failed(string message)
        {
            return new ShopPurchaseResult(false, message, new List<ItemAmountEntry>());
        }

        #endregion
    }
}
