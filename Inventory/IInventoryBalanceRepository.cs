using System;
using System.Numerics;
using FakeMG.Framework;

namespace FakeMG.Inventory
{
    public interface IInventoryBalanceRepository
    {
        event Action<InventoryChange> OnBalanceChanged;

        BigInteger GetBalance(IdentitySO itemSo);
        bool TrySpend(IdentitySO itemSo, BigInteger amount);
        void Add(IdentitySO itemSo, BigInteger amount);
        void SetBalance(IdentitySO itemSo, BigInteger amount);
    }
}
