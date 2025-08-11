using System;

namespace FakeMG.FakeMGFramework.Gacha
{
    [Serializable]
    public class GachaRewardData
    {
        public ItemBaseSO rewardObject;
        public float probability;
        public int amount = 1;
    }
}