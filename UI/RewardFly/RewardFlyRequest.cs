using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Framework.UI.RewardFly
{
    public readonly struct RewardFlyRequest
    {
        public readonly IdentitySO IdentitySO;
        public readonly RewardFlyTokenView TokenPrefab;
        public readonly int Amount;
        public readonly Transform SourceTransform;
        public readonly Transform TargetTransform;
        public readonly RewardFlySpace FlySpace;
        public readonly Action OnTokenArrived;

        public RewardFlyRequest(
            IdentitySO identitySO,
            RewardFlyTokenView tokenPrefab,
            int amount,
            Transform sourceTransform,
            Transform targetTransform,
            RewardFlySpace flySpace,
            Action onTokenArrived)
        {
            IdentitySO = identitySO;
            TokenPrefab = tokenPrefab;
            Amount = amount;
            SourceTransform = sourceTransform;
            TargetTransform = targetTransform;
            FlySpace = flySpace;
            OnTokenArrived = onTokenArrived;
        }
    }
}
