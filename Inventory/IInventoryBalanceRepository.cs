using System;
using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Numbers;

namespace FakeMG.Inventory
{
    public interface IInventoryBalanceRepository
    {
        event Action<InventoryChange> OnBalanceChanged;

        GameNumber GetBalance(IdentitySO itemSo);
        bool TrySpend(IdentitySO itemSo, GameNumber amount);
        bool TrySpend(IReadOnlyList<ItemAmountEntry> entries);
        void Add(IdentitySO itemSo, GameNumber amount);
        IReadOnlyList<ItemAmountEntry> Add(IReadOnlyList<ItemAmountEntry> entries);
        void SetBalance(IdentitySO itemSo, GameNumber amount);
    }
}
