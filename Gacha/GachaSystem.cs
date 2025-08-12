using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.FakeMGFramework.Gacha
{
    public class GachaSystem : MonoBehaviour
    {
        [SerializeField, ValidateInput("ValidateProbabilities", "All probabilities must add up to 1.0")]
        private List<GachaRewardData> rewards;

        public List<GachaRewardData> Rewards => rewards;

        private bool ValidateProbabilities()
        {
            if (rewards == null || rewards.Count == 0) return true;

            float totalProbability = rewards.Sum(reward => reward.probability);
            return Mathf.Approximately(totalProbability, 1f);
        }

        public int ChooseRandomReward()
        {
            float randomValue = Random.value;
            float cumulativeProbability = 0f;

            for (int i = 0; i < rewards.Count; i++)
            {
                cumulativeProbability += rewards[i].probability;
                if (randomValue <= cumulativeProbability)
                {
                    Debug.Log($"Chosen reward index: {i}, Name: {rewards[i].rewardObject.ItemName}");
                    return i;
                }
            }

            // Fallback in case of rounding errors
            return rewards.Count - 1;
        }
    }
}