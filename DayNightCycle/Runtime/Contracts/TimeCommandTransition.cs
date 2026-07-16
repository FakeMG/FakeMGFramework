using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Configures real-time duration and easing for a time command.
    /// </summary>
    [Serializable]
    public readonly struct TimeCommandTransition
    {
        public float DurationSeconds { get; }
        public AnimationCurve EasingCurve { get; }
        public bool UsesProfileDefault => DurationSeconds < 0f;

        public TimeCommandTransition(float durationSeconds, AnimationCurve easingCurve = null)
        {
            DurationSeconds = durationSeconds;
            EasingCurve = easingCurve;
        }

        #region Public Methods

        public static TimeCommandTransition ProfileDefault()
        {
            return new TimeCommandTransition(-1f);
        }

        #endregion
    }
}
