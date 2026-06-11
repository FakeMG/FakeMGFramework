using System;
using System.Numerics;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Inventory
{
    [Serializable]
    public class InventoryBalanceEntry
    {
        [SerializeField] private IdentitySO _item;
        [SerializeField] private string _amountText = "0";

        public IdentitySO Item => _item;
        public BigInteger Amount => (BigInteger)BigNumber.ParseOrDefault(_amountText, BigNumber.Zero);

        public void SetAmount(BigInteger amount)
        {
            _amountText = amount.ToString();
        }
    }
}
