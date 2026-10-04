using System;
using System.Collections.Generic;
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
    public sealed class InventoryStateSaveable : MonoBehaviour, ISaveable
    {
        public const string SAVE_ID = nameof(InventoryStateSaveable);

        [SerializeField] private List<ItemAmountEntry> _initialBalances = new();

        private InventoryBalanceState _state;

        public string SaveId => SAVE_ID;

        #region Public Methods

        [Inject]
        public void Construct(InventoryBalanceState state)
        {
            _state = state;
        }

        public object CaptureState()
        {
            return _state.CaptureState();
        }

        public bool TryValidateState(object state, out string failureReason)
        {
            if (state is InventoryData inventoryData && inventoryData.AmountByItemId != null)
            {
                failureReason = string.Empty;
                return true;
            }

            failureReason = "Inventory state is invalid or in an unsupported legacy format.";
            return false;
        }

        public void RestoreState(object data)
        {
            _state.RestoreState((InventoryData)data);
        }

        public void RestoreDefaultState()
        {
            _state.RestoreDefaultState(_initialBalances);
        }

        #endregion
    }
}
