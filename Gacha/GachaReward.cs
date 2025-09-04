using System;

namespace FakeMG.Framework.Gacha
{
    [Serializable]
    public class GachaRewardData
    {
        public ItemSO rewardObject;
        public float probability;
        public int amount = 1;
    }
}