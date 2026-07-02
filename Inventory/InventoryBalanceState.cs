using System;
using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Numbers;

namespace FakeMG.Inventory
{
    /// <summary>
    /// Single source of truth for inventory balances. Injected directly so callers depend on this (or its
    /// IInventoryBalanceRepository interface) rather than on InventoryDataManager, which is only the thin
    /// Saveable adapter that loads and saves this state.
    /// </summary>
    public sealed class InventoryBalanceState : IInventoryBalanceRepository
    {
        private readonly Dictionary<string, GameNumber> _amountByItemId = new();

        public event Action<InventoryChange> OnBalanceChanged;
        public event Action BalancesReloaded;

        #region Public Methods

        public GameNumber GetBalance(IdentitySO itemSo)
        {
            return TryGetItemId(itemSo, out string itemId) ? GetBalance(itemId) : GameNumber.Zero;
        }

        public bool TrySpend(IdentitySO itemSo, GameNumber amount)
        {
            if (amount <= GameNumber.Zero)
            {
                Echo.Warning($"Spend amount must be greater than zero. Received: {amount}.");
                return false;
            }

            if (!TryGetItemId(itemSo, out string itemId))
            {
                return false;
            }

            GameNumber oldAmount = GetBalance(itemId);
            if (oldAmount < amount)
            {
                return false;
            }

            GameNumber newAmount = oldAmount - amount;
            _amountByItemId[itemId] = newAmount;
            NotifyBalanceChanged(itemSo, oldAmount, newAmount);
            return true;
        }

        public bool TrySpend(IReadOnlyList<ItemAmountEntry> entries)
        {
            if (entries == null)
            {
                Echo.Warning("Skipped an inventory spend batch because it was null.");
                return false;
            }

            Dictionary<string, GameNumber> spendAmountByItemId = new();
            Dictionary<string, IdentitySO> itemSOByItemId = new();

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                ItemAmountEntry entry = entries[entryIndex];
                if (!TryGetBatchEntry(entry, entryIndex, out IdentitySO itemSo, out string itemId, out GameNumber amount))
                {
                    continue;
                }

                AddToBatch(spendAmountByItemId, itemId, amount);
                itemSOByItemId[itemId] = itemSo;
            }

            foreach ((string itemId, GameNumber spendAmount) in spendAmountByItemId)
            {
                if (GetBalance(itemId) < spendAmount)
                {
                    return false;
                }
            }

            foreach ((string itemId, GameNumber spendAmount) in spendAmountByItemId)
            {
                IdentitySO itemSo = itemSOByItemId[itemId];
                GameNumber oldAmount = GetBalance(itemId);
                GameNumber newAmount = oldAmount - spendAmount;

                _amountByItemId[itemId] = newAmount;
                NotifyBalanceChanged(itemSo, oldAmount, newAmount);
            }

            return true;
        }

        public void Add(IdentitySO itemSo, GameNumber amount)
        {
            if (amount <= GameNumber.Zero)
            {
                Echo.Warning($"Added amount must be greater than zero. Received: {amount}.");
                return;
            }

            if (!TryGetItemId(itemSo, out string itemId))
            {
                return;
            }

            AddInternal(itemSo, itemId, amount);
        }

        public IReadOnlyList<ItemAmountEntry> Add(IReadOnlyList<ItemAmountEntry> entries)
        {
            List<ItemAmountEntry> addedEntries = new();

            if (entries == null)
            {
                Echo.Warning("Skipped an inventory add batch because it was null.");
                return addedEntries;
            }

            Dictionary<string, GameNumber> addAmountByItemId = new();
            Dictionary<string, IdentitySO> itemSOByItemId = new();

            for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
            {
                ItemAmountEntry entry = entries[entryIndex];
                if (!TryGetBatchEntry(entry, entryIndex, out IdentitySO itemSo, out string itemId, out GameNumber amount))
                {
                    continue;
                }

                AddToBatch(addAmountByItemId, itemId, amount);
                itemSOByItemId[itemId] = itemSo;
                addedEntries.Add(entry);
            }

            foreach ((string itemId, GameNumber addAmount) in addAmountByItemId)
            {
                AddInternal(itemSOByItemId[itemId], itemId, addAmount);
            }

            return addedEntries;
        }

        public void SetBalance(IdentitySO itemSo, GameNumber amount)
        {
            if (!TryGetItemId(itemSo, out string itemId))
            {
                return;
            }

            GameNumber oldAmount = GetBalance(itemId);
            GameNumber newAmount = GameNumber.Max(GameNumber.Zero, amount);
            _amountByItemId[itemId] = newAmount;
            NotifyBalanceChanged(itemSo, oldAmount, newAmount);
        }

        public InventoryData CaptureState()
        {
            Dictionary<string, string> serialized = new(_amountByItemId.Count);
            foreach ((string itemId, GameNumber amount) in _amountByItemId)
            {
                serialized[itemId] = amount.ToString();
            }

            return new InventoryData
            {
                AmountByItemId = serialized,
            };
        }

        public void RestoreState(InventoryData restoredData)
        {
            _amountByItemId.Clear();

            if (restoredData?.AmountByItemId == null)
            {
                Echo.Warning("Restored inventory data was null or missing balances. Inventory is restored as empty.");
                NotifyBalancesReloaded();
                return;
            }

            foreach ((string itemId, string amountText) in restoredData.AmountByItemId)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    Echo.Warning("Encountered an inventory entry with an empty item id while restoring state. Entry is skipped.");
                    continue;
                }

                if (!GameNumber.TryParse(amountText, out GameNumber amount))
                {
                    Echo.Warning($"Inventory entry '{itemId}' has an unparsable amount '{amountText}'. Entry is skipped.");
                    continue;
                }

                _amountByItemId[itemId] = GameNumber.Max(GameNumber.Zero, amount);
            }

            NotifyBalancesReloaded();
        }

        public void RestoreDefaultState(IReadOnlyList<ItemAmountEntry> initialBalances)
        {
            _amountByItemId.Clear();

            if (initialBalances == null)
            {
                Echo.Warning("Skipped restoring default inventory state because initial balances were null.");
                NotifyBalancesReloaded();
                return;
            }

            for (int entryIndex = 0; entryIndex < initialBalances.Count; entryIndex++)
            {
                ItemAmountEntry entry = initialBalances[entryIndex];
                if (!TryGetBatchEntry(entry, entryIndex, out _, out string itemId, out GameNumber amount))
                {
                    continue;
                }

                _amountByItemId[itemId] = GameNumber.Max(GameNumber.Zero, amount);
            }

            NotifyBalancesReloaded();
        }

        #endregion

        #region Private Methods

        private GameNumber GetBalance(string itemId)
        {
            return _amountByItemId.TryGetValue(itemId, out GameNumber amount) ? amount : GameNumber.Zero;
        }

        private void AddInternal(IdentitySO itemSo, string itemId, GameNumber amount)
        {
            GameNumber oldAmount = GetBalance(itemId);
            GameNumber newAmount = oldAmount + amount;

            _amountByItemId[itemId] = newAmount;
            NotifyBalanceChanged(itemSo, oldAmount, newAmount);
        }

        private void NotifyBalanceChanged(IdentitySO itemSo, GameNumber oldAmount, GameNumber newAmount)
        {
            OnBalanceChanged?.Invoke(new InventoryChange(itemSo, oldAmount, newAmount));
        }

        private void NotifyBalancesReloaded()
        {
            BalancesReloaded?.Invoke();
        }

        private bool TryGetItemId(IdentitySO itemSo, out string itemId)
        {
            itemId = null;

            if (!itemSo)
            {
                Echo.Warning("Inventory operation was requested with a null item reference.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(itemSo.Id))
            {
                Echo.Warning($"Inventory item '{itemSo.name}' has an empty id.");
                return false;
            }

            itemId = itemSo.Id;
            return true;
        }

        private bool TryGetBatchEntry(
            ItemAmountEntry entry,
            int entryIndex,
            out IdentitySO itemSo,
            out string itemId,
            out GameNumber amount)
        {
            itemSo = null;
            itemId = null;
            amount = GameNumber.Zero;

            if (entry == null)
            {
                Echo.Warning($"Skipped inventory batch entry at index {entryIndex} because it was null.");
                return false;
            }

            itemSo = entry.IdentitySO;
            amount = entry.Amount;

            if (!TryGetItemId(itemSo, out itemId))
            {
                Echo.Warning($"Skipped inventory batch entry at index {entryIndex} because its item identity is invalid.");
                return false;
            }

            if (amount <= GameNumber.Zero)
            {
                Echo.Warning($"Skipped inventory batch entry for item '{itemSo.name}' because it has a non-positive amount: {amount}.");
                return false;
            }

            return true;
        }

        private static void AddToBatch(Dictionary<string, GameNumber> amountByItemId, string itemId, GameNumber amount)
        {
            if (!amountByItemId.TryAdd(itemId, amount))
            {
                amountByItemId[itemId] += amount;
            }
        }

        #endregion
    }
}