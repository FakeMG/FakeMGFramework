using System;
using FakeMG.Framework;
using FakeMG.Numbers;
using UnityEngine;

namespace FakeMG.Inventory
{
    [Serializable]
    public class ItemAmountEntry
    {
        [SerializeField] private IdentitySO _item;
        [SerializeField] private string _amountText = "0";

        // Set when an entry is built at runtime (e.g. scaled drops, merged bundle grants); overrides the authored text.
        private readonly GameNumber? _runtimeAmount;

        public ItemAmountEntry()
        {
        }

        public ItemAmountEntry(IdentitySO identitySO, GameNumber amount)
        {
            _item = identitySO;
            _runtimeAmount = amount;
        }

        public IdentitySO IdentitySO => _item;
        public GameNumber Amount => _runtimeAmount ?? GameNumber.ParseOrDefault(_amountText, GameNumber.Zero);
    }
}
