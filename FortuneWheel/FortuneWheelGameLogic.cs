using System;
using System.Globalization;
using FakeMG.Framework.Gacha;
using UnityEngine;

namespace FakeMG.Framework.FortuneWheel
{
    public class FortuneWheelGameLogic : MonoBehaviour
    {
        [SerializeField] private GachaSystem _gachaSystem;
        [SerializeField] public float CooldownDurationMinutes = 30f;

        public const string LAST_SPIN_KEY = "LastSpin";

        private int _chosenRewardIndex;
        private DateTime _lastSpinTime;

        public int ChosenRewardIndex => _chosenRewardIndex;

        public event Action OnSpinStarted;

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

            _chosenRewardIndex = _gachaSystem.ChooseRandomReward();

            _lastSpinTime = DateTime.Now;
            PlayerPrefs.SetString(LAST_SPIN_KEY, _lastSpinTime.ToString(CultureInfo.InvariantCulture));
            PlayerPrefs.Save();

            OnSpinStarted?.Invoke();

            //TODO: The reward should be claimed right away to avoid loss if the app crashes
        }

        public void GrantReward(int multiplier)
        {
            // TODO: Delegate reward granting logic to another class / event
        }

        public bool IsInCooldown()
        {
            return (DateTime.Now - _lastSpinTime).TotalMinutes < CooldownDurationMinutes;
        }

        public TimeSpan GetRemainingCooldownTime()
        {
            if (!IsInCooldown()) return TimeSpan.Zero;

            return TimeSpan.FromMinutes(CooldownDurationMinutes) - (DateTime.Now - _lastSpinTime);
        }
    }
}