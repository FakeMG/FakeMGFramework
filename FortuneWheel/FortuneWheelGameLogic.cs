using System;
using System.Globalization;
using FakeMG.Framework.Gacha;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.Framework.FortuneWheel
{
    public class FortuneWheelGameLogic : MonoBehaviour
    {
        [SerializeField] private GachaSystem gachaSystem;
        [SerializeField] public float cooldownDurationMinutes = 30f;

        public const string LAST_SPIN_KEY = "LastSpin";

        private int _chosenRewardIndex;
        private DateTime _lastSpinTime;

        public int ChosenRewardIndex => _chosenRewardIndex;

        public UnityEvent onSpinStarted;

        private void Start()
        {
            string savedLastSpinTime = PlayerPrefs.GetString(LAST_SPIN_KEY, "");
            if (string.IsNullOrEmpty(savedLastSpinTime))
            {
                _lastSpinTime = DateTime.MinValue;
            }
            else
            {
                _lastSpinTime = DateTime.Parse(savedLastSpinTime, CultureInfo.InvariantCulture);
            }
        }

        public void Spin()
        {
            if (IsInCooldown()) return;

            _chosenRewardIndex = gachaSystem.ChooseRandomReward();

            _lastSpinTime = DateTime.Now;
            PlayerPrefs.SetString(LAST_SPIN_KEY, _lastSpinTime.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();

            onSpinStarted?.Invoke();
        }

        public void ClaimReward(int multiplier)
        {
            // TODO: Implement reward claiming logic
            // PlayerDataManager.Instance.AddToInventory(rewards[_chosenRewardIndex].rewardObject,
            //     rewards[_chosenRewardIndex].amount * multiplier);
        }

        public bool IsInCooldown()
        {
            return (DateTime.Now - _lastSpinTime).TotalMinutes < cooldownDurationMinutes;
        }

        public TimeSpan GetRemainingCooldownTime()
        {
            if (!IsInCooldown()) return TimeSpan.Zero;

            return TimeSpan.FromMinutes(cooldownDurationMinutes) - (DateTime.Now - _lastSpinTime);
        }
    }
}