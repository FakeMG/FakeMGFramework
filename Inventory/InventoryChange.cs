using System.Numerics;
using FakeMG.Framework;

namespace FakeMG.Inventory
{
    public readonly struct InventoryChange
    {
        public readonly IdentitySO IdentitySO;
        public readonly BigInteger OldCount;
        public readonly BigInteger NewCount;

        public InventoryChange(IdentitySO identitySO, BigInteger oldCount, BigInteger newCount)
        {
            IdentitySO = identitySO;
            OldCount = oldCount;
            NewCount = newCount;
        }
    }
}
