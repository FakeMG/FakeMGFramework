using System.Collections.Generic;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Shop.Config
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SHOP + "/VerticalCardList/BundleListingSO")]
    public class BundleListingSO : ShopListingSO
    {
        [SerializeField] private IdentitySO _currencyItemGranted;
        [SerializeField] private int _currencyAmountGranted;
        [SerializeField] private Dictionary<IdentitySO, int> _boostersGranted = new();
        [SerializeField] private Dictionary<IdentitySO, int> _otherItemsGranted = new();

        #region Public Properties
        public IdentitySO CurrencyItemGranted => _currencyItemGranted;
        public int CurrencyAmountGranted => _currencyAmountGranted;
        public IReadOnlyDictionary<IdentitySO, int> BoostersGranted => _boostersGranted;
        public IReadOnlyDictionary<IdentitySO, int> OtherItemsGranted => _otherItemsGranted;

        #endregion

        #region Public Methods

        public override IReadOnlyDictionary<IdentitySO, int> GetAllItemsGranted()
        {
            var mergedItemsByItem = new Dictionary<IdentitySO, int>();

            MergeInto(mergedItemsByItem, base.GetAllItemsGranted());
            MergeAmount(mergedItemsByItem, _currencyItemGranted, _currencyAmountGranted);

            MergeInto(mergedItemsByItem, _boostersGranted);
            MergeInto(mergedItemsByItem, _otherItemsGranted);
            return mergedItemsByItem;
        }

        #endregion

        #region Private Methods

        private static void MergeInto(Dictionary<IdentitySO, int> mergedItemsByItem, IReadOnlyDictionary<IdentitySO, int> sourceItemsByItem)
        {
            foreach ((IdentitySO itemSo, int amount) in sourceItemsByItem)
            {
                MergeAmount(mergedItemsByItem, itemSo, amount);
            }
        }

        private static void MergeAmount(Dictionary<IdentitySO, int> mergedItemsByItem, IdentitySO itemSo, int amount)
        {
            if (!itemSo || amount <= 0)
            {
                return;
            }

            int currentAmount = mergedItemsByItem.TryGetValue(itemSo, out int existingAmount) ? existingAmount : 0;
            mergedItemsByItem[itemSo] = currentAmount + amount;
        }

        #endregion
    }
}
