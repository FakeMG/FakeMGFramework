using System;

namespace FakeMG.Framework.Timer
{
    /// <summary>
    /// Remaining time never goes below zero. Invalid durations become zero, and invalid deltas leave the timer unchanged.
    /// </summary>
    public class Timer
    {
        public float TimeToWait { get; private set; }
        public float CurrentTimeLeftInSeconds { get; private set; }

        public Timer(float timeToWait)
        {
            SetTime(timeToWait);
        }

        public bool Tick(float deltaTime)
        {
            if (float.IsFinite(deltaTime) && deltaTime > 0f)
            {
                CurrentTimeLeftInSeconds = Math.Max(0f, CurrentTimeLeftInSeconds - deltaTime);
            }

            return CurrentTimeLeftInSeconds <= 0f;
        }

        public void SetTime(float time)
        {
            float validTime = float.IsFinite(time) ? Math.Max(0f, time) : 0f;
            TimeToWait = validTime;
            CurrentTimeLeftInSeconds = validTime;
        }

        public void AddTime(float time)
        {
            if (float.IsFinite(time))
            {
                CurrentTimeLeftInSeconds = Math.Max(0f, CurrentTimeLeftInSeconds + time);
            }
        }
    }
}
