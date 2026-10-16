namespace FakeMG.Shop.Services.Purchase
{
    public interface IShopPurchaseConfirmation
    {
        void ConfirmPurchase(string transactionId);
    }
}
