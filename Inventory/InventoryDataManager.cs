using System;
using System.Collections.Generic;
using System.Numerics;
using FakeMG.Framework;
using FakeMG.SaveLoad;
using UnityEngine;

namespace FakeMG.Inventory
{
    [Serializable]
    public class InventoryData
    {
        // BigInteger is serialized as its decimal string so the save format stays robust
        // across magnitudes beyond int/long.
        public Dictionary<string, string> AmountByItemId = new();
    }

    public class InventoryDataManager : Saveable, IInventoryBalanceRepository
    {
        [SerializeField] private List<InventoryBalanceEntry> _initialBalances = new();

        public event Action<InventoryChange> OnBalanceChanged;
        public event Action BalancesReloaded;

        private readonly Dictionary<string, BigInteger> _amountByItemId = new();

        #region Public Methods

        public BigInteger GetBalance(IdentitySO itemSo)
        {
            if (!TryGetItemId(itemSo, out string itemId))
            {
                return BigInteger.Zero;
            }

            return _amountByItemId.TryGetValue(itemId, out BigInteger amount) ? amount : BigInteger.Zero;
        }

        public bool TrySpend(IdentitySO itemSo, BigInteger amount)
        {
            if (amount <= BigInteger.Zero)
            {
                Echo.Warning($"Spend amount must be greater than zero. Received: {amount}.");
                return false;
            }

            if (!TryGetItemId(itemSo, out string itemId))
            {
                return false;
            }

            if (!_amountByItemId.TryGetValue(itemId, out BigInteger currentAmount) || currentAmount < amount)
            {
                return false;
            }

            BigInteger oldAmount = currentAmount;
            BigInteger newAmount = oldAmount - amount;
            _amountByItemId[itemId] = newAmount;
            NotifyBalanceChanged(itemSo, oldAmount, newAmount);
            return true;
        }

        public void Add(IdentitySO itemSo, BigInteger amount)
        {
            if (amount <= BigInteger.Zero)
            {
                Echo.Warning($"Added amount must be greater than zero. Received: {amount}.");
                return;
            }

            if (!TryGetItemId(itemSo, out string itemId))
            {
                return;
            }

            BigInteger oldAmount = _amountByItemId.TryGetValue(itemId, out BigInteger existingAmount) ? existingAmount : BigInteger.Zero;
            BigInteger newAmount = oldAmount + amount;
            _amountByItemId[itemId] = newAmount;
            NotifyBalanceChanged(itemSo, oldAmount, newAmount);
        }

        public void SetBalance(IdentitySO itemSo, BigInteger amount)
        {
            if (!TryGetItemId(itemSo, out string itemId))
            {
                return;
            }

            BigInteger oldAmount = _amountByItemId.TryGetValue(itemId, out BigInteger existingAmount) ? existingAmount : BigInteger.Zero;
            BigInteger newAmount = BigInteger.Max(BigInteger.Zero, amount);
            _amountByItemId[itemId] = newAmount;
            NotifyBalanceChanged(itemSo, oldAmount, newAmount);
        }

        public override object CaptureState()
        {
            Dictionary<string, string> serialized = new(_amountByItemId.Count);
            foreach ((string itemId, BigInteger amount) in _amountByItemId)
            {
                serialized[itemId] = amount.ToString();
            }

            return new InventoryData
            {
                AmountByItemId = serialized,
            };
        }

        public override void RestoreState(object data)
        {
            if (!StateRestoreUtility.TryRestore(data, out InventoryData restoredData) || restoredData.AmountByItemId == null)
            {
                Echo.Warning("Inventory state data is invalid or in a legacy format. Restoring default inventory state.");
                RestoreDefaultState();
                return;
            }

            _amountByItemId.Clear();
            foreach ((string itemId, string amountText) in restoredData.AmountByItemId)
            {
                if (string.IsNullOrWhiteSpace(itemId))
                {
                    Echo.Warning("Encountered an inventory entry with an empty item id while restoring state. Entry is skipped.");
                    continue;
                }

                if (!BigInteger.TryParse(amountText, out BigInteger amount))
                {
                    Echo.Warning($"Inventory entry '{itemId}' has an unparsable amount '{amountText}'. Entry is skipped.");
                    continue;
                }

                _amountByItemId[itemId] = BigInteger.Max(BigInteger.Zero, amount);
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

                _amountByItemId[itemId] = BigInteger.Max(BigInteger.Zero, entry.Amount);
            }

            NotifyBalancesReloaded();
        }

        #endregion

        #region Private Methods

        private void NotifyBalanceChanged(IdentitySO itemSo, BigInteger oldAmount, BigInteger newAmount)
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
