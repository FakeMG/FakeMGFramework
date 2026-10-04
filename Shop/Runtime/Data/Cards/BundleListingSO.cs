using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Inventory;
using FakeMG.Numbers;
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

        public override IReadOnlyList<ItemAmountEntry> GetAllItemsGranted()
        {
            var mergedAmountByItem = new Dictionary<IdentitySO, GameNumber>();

            MergeInto(mergedAmountByItem, base.GetAllItemsGranted());
            MergeAmount(mergedAmountByItem, _currencyItemGranted, _currencyAmountGranted);

            MergeInto(mergedAmountByItem, _boostersGranted);
            MergeInto(mergedAmountByItem, _otherItemsGranted);

            var mergedEntries = new List<ItemAmountEntry>(mergedAmountByItem.Count);
            foreach ((IdentitySO itemSo, GameNumber amount) in mergedAmountByItem)
            {
                mergedEntries.Add(new ItemAmountEntry(itemSo, amount));
            }

            return mergedEntries;
        }

        #endregion

        #region Private Methods

        private static void MergeInto(Dictionary<IdentitySO, GameNumber> mergedAmountByItem, IReadOnlyList<ItemAmountEntry> sourceEntries)
        {
            for (int entryIndex = 0; entryIndex < sourceEntries.Count; entryIndex++)
            {
                ItemAmountEntry entry = sourceEntries[entryIndex];
                MergeAmount(mergedAmountByItem, entry.IdentitySO, entry.Amount);
            }
        }

        private static void MergeInto(Dictionary<IdentitySO, GameNumber> mergedAmountByItem, IReadOnlyDictionary<IdentitySO, int> sourceItemsByItem)
        {
            foreach ((IdentitySO itemSo, int amount) in sourceItemsByItem)
            {
                MergeAmount(mergedAmountByItem, itemSo, amount);
            }
        }

        private static void MergeAmount(Dictionary<IdentitySO, GameNumber> mergedAmountByItem, IdentitySO itemSo, GameNumber amount)
        {
            if (!itemSo || amount <= GameNumber.Zero)
            {
                return;
            }

            GameNumber currentAmount = mergedAmountByItem.TryGetValue(itemSo, out GameNumber existingAmount) ? existingAmount : GameNumber.Zero;
            mergedAmountByItem[itemSo] = currentAmount + amount;
        }

        #endregion
    }
}
