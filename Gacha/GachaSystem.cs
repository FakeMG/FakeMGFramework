using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework.Gacha
{
    public class GachaSystem : MonoBehaviour
    {
        [SerializeField, ValidateInput("ValidateProbabilities", "All probabilities must add up to 1.0")]
        private List<GachaRewardData> _rewards;

        public List<GachaRewardData> Rewards => _rewards;

        private bool ValidateProbabilities()
        {
            if (_rewards == null || _rewards.Count == 0) return true;

            float totalProbability = _rewards.Sum(reward => reward.Probability);
            return Mathf.Approximately(totalProbability, 1f);
        }

        public int ChooseRandomReward()
        {
            float randomValue = Random.value;
            float cumulativeProbability = 0f;

            for (int i = 0; i < _rewards.Count; i++)
            {
                cumulativeProbability += _rewards[i].Probability;
                if (randomValue <= cumulativeProbability)
                {
                    Debug.Log($"Chosen reward index: {i}, Name: {_rewards[i].RewardObject.ItemName}");
                    return i;
                }
            }

            // Fallback in case of rounding errors
            return _rewards.Count - 1;
        }
    }
}