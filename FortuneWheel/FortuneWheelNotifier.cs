﻿using System;
using System.Globalization;
using UnityEngine;

namespace FakeMG.Framework.FortuneWheel
{
    public class FortuneWheelNotifier : MonoBehaviour
    {
        [SerializeField] private FortuneWheelGameLogic fortuneWheelGameLogic;
        [SerializeField] private GameObject notificationIcon;

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

            notificationIcon.SetActive(!fortuneWheelGameLogic.IsInCooldown());
        }
    }
}