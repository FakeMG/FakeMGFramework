using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Inventory
{
    [Serializable]
    public class InventoryBalanceEntry
    {
        [SerializeField] private IdentitySO _item;
        [SerializeField] private int _amount;

        public IdentitySO Item => _item;
        public int Amount => _amount;

        public void SetAmount(int amount)
        {
            // TODO: Set the amount for this item entry.
            _amount = amount;
        }
    }
}
