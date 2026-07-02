using System;
using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.SaveLoad;
using UnityEngine;
using VContainer;

namespace FakeMG.Inventory
{
    [Serializable]
    public class InventoryData
    {
        // GameNumber is serialized as its decimal string so the save format stays robust
        // across magnitudes beyond int/long.
        public Dictionary<string, string> AmountByItemId = new();
    }

    /// <summary>
    /// Persists inventory balances across sessions. Delegates all state to InventoryBalanceState, which is
    /// injected so other systems can resolve it directly (or via IInventoryBalanceRepository) without going
    /// through this Saveable.
    /// </summary>
    public class InventoryDataManager : Saveable
    {
        [SerializeField] private List<ItemAmountEntry> _initialBalances = new();

        private InventoryBalanceState _state;

        #region Public Methods

        [Inject]
        public void Construct(InventoryBalanceState state)
        {
            _state = state;
        }

        public override object CaptureState()
        {
            return _state.CaptureState();
        }

        public override void RestoreState(object data)
        {
            if (!StateRestoreUtility.TryRestore(data, out InventoryData restoredData) || restoredData.AmountByItemId == null)
            {
                Echo.Warning("Inventory state data is invalid or in a legacy format. Restoring default inventory state.");
                RestoreDefaultState();
                return;
            }

            _state.RestoreState(restoredData);
        }

        public override void RestoreDefaultState()
        {
            _state.RestoreDefaultState(_initialBalances);
        }

        #endregion
    }
}
