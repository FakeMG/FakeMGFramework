using System;
using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.SaveLoad;
using UnityEngine;

namespace FakeMG.Inventory
{
    [Serializable]
    public class InventoryData
    {
        public Dictionary<string, int> AmountByItemId = new();
    }

    public class InventoryDataManager : Saveable, IInventoryBalanceRepository
    {
        [SerializeField] private List<InventoryBalanceEntry> _initialBalances = new();

        public event Action<InventoryChange> OnBalanceChanged;
        public event Action BalancesReloaded;

        private readonly Dictionary<string, int> _amountByItemId = new();

        #region Public Methods

        public int GetBalance(IdentitySO itemSo)
        {
            if (!TryGetItemId(itemSo, out string itemId))
            {
                return 0;
            }

            return _amountByItemId.TryGetValue(itemId, out int amount) ? amount : 0;
        }

        public bool TrySpend(IdentitySO itemSo, int amount)
        {
            if (amount <= 0)
            {
                Echo.Warning($"Spend amount must be greater than zero. Received: {amount}.");
                return false;
            }

            if (!TryGetItemId(itemSo, out string itemId))
            {
                return false;
            }

            if (!_amountByItemId.TryGetValue(itemId, out int currentAmount) || currentAmount < amount)
            {
                return false;
            }

            int oldAmount = currentAmount;
            int newAmount = oldAmount - amount;
            _amountByItemId[itemId] = newAmount;
            NotifyBalanceChanged(itemSo, oldAmount, newAmount);
            return true;
        }

        public void Add(IdentitySO itemSo, int amount)
        {
            if (amount <= 0)
            {
                Echo.Warning($"Added amount must be greater than zero. Received: {amount}.");
                return;
            }

            if (!TryGetItemId(itemSo, out string itemId))
            {
                return;
            }

            int oldAmount = _amountByItemId.TryGetValue(itemId, out int existingAmount) ? existingAmount : 0;
            int newAmount = oldAmount + amount;
            _amountByItemId[itemId] = newAmount;
            NotifyBalanceChanged(itemSo, oldAmount, newAmount);
        }

        public void SetBalance(IdentitySO itemSo, int amount)
        {
            if (!TryGetItemId(itemSo, out string itemId))
            {
                return;
            }

            int oldAmount = _amountByItemId.TryGetValue(itemId, out int existingAmount) ? existingAmount : 0;
            int newAmount = Mathf.Max(0, amount);
            _amountByItemId[itemId] = newAmount;
            NotifyBalanceChanged(itemSo, oldAmount, newAmount);
        }

        public override object CaptureState()
        {
            return new InventoryData
            {
                AmountByItemId = new Dictionary<string, int>(_amountByItemId),
            };
        }

        public override void RestoreState(object data)
        {
            if (!StateRestoreUtility.TryRestore(data, out InventoryData restoredData) || restoredData.AmountByItemId == null)
            {
                Echo.Warning("Inventory state data is invalid. Restoring default inventory state.");
                RestoreDefaultState();
                return;
            }

            _amountByItemId.Clear();
            foreach ((string itemId, int amount) in restoredData.AmountByItemId)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    Echo.Warning("Encountered an inventory entry with an empty item id while restoring state. Entry is skipped.");
                    continue;
                }

                _amountByItemId[itemId] = Mathf.Max(0, amount);
            }

            NotifyBalancesReloaded();
        }

        public override void RestoreDefaultState()
        {
            _amountByItemId.Clear();

            for (int entryIndex = 0; entryIndex < _initialBalances.Count; entryIndex++)
            {
                InventoryBalanceEntry entry = _initialBalances[entryIndex];
                if (entry == null)
                {
                    Echo.Warning($"Inventory default entry at index {entryIndex} is null. Entry is skipped.");
                    continue;
                }

                if (!TryGetItemId(entry.Item, out string itemId))
                {
                    continue;
                }

                _amountByItemId[itemId] = Mathf.Max(0, entry.Amount);
            }

            NotifyBalancesReloaded();
        }

        #endregion

        #region Private Methods

        private void NotifyBalanceChanged(IdentitySO itemSo, int oldAmount, int newAmount)
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

        #endregion
    }
}
