using System;

namespace FakeMG.Framework.Gacha
{
    [Serializable]
    public class GachaRewardData
    {
        public ItemSO RewardObject;
        public float Probability;
        public int Amount = 1;
    }
}