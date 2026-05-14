using System;
using FakeMG.Framework;

namespace FakeMG.Inventory
{
    public interface IInventoryBalanceRepository
    {
        event Action<IdentitySO, int> BalanceChanged;

        int GetBalance(IdentitySO itemSo);
        bool TrySpend(IdentitySO itemSo, int amount);
        void SetBalance(IdentitySO itemSo, int amount);
    }
}
