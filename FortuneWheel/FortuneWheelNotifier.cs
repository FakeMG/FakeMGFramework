using System;
using System.Globalization;
using UnityEngine;

namespace FakeMG.Framework.FortuneWheel
{
    public class FortuneWheelNotifier : MonoBehaviour
    {
        [SerializeField] private FortuneWheelGameLogic _fortuneWheelGameLogic;
        [SerializeField] private GameObject _notificationIcon;

        private DateTime _lastSpinTime;

        private void Start()
        {
            UpdateNotificationIcon();
        }

        public void UpdateNotificationIcon()
        {
            string savedLastSpinTime = PlayerPrefs.GetString(FortuneWheelGameLogic.LAST_SPIN_KEY, "");
            if (string.IsNullOrEmpty(savedLastSpinTime))
            {
                _lastSpinTime = DateTime.MinValue;
            }
            else
            {
                _lastSpinTime = DateTime.Parse(savedLastSpinTime, CultureInfo.InvariantCulture);
            }

            _notificationIcon.SetActive(!_fortuneWheelGameLogic.IsInCooldown());
        }
    }
}