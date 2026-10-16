using System;

namespace FakeMG.Shop.RuntimeData
{
    [Serializable]
    public class ShopRewardDelivery
    {
        public string TransactionId;
        public string ListingId;
        public bool IsCommitted;
    }
}
