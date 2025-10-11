using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework.Timer
{
    public class CountDownTimer : MonoBehaviour
    {
        [SerializeField] private float warningPeriod = 5f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private float timeToWait;

        public float CurrentTimeLeftInSeconds { get; private set; }
        private float _timeSinceLastSecond;

        private bool _isTimerRunning;
        private bool _wasRunningBeforePause;

        public bool IsTimerRunning => _isTimerRunning;
        public bool WasRunningBeforePause => _wasRunningBeforePause;

        public event Action OnTimerStart;
        public event Action<int> OnSecondReducedEvent;
        public event Action OnTimerEnd;
        public event Action<int> OnWarningSecondReducedEvent;

        private void Start()
        {
            CurrentTimeLeftInSeconds = timeToWait;

            if (playOnStart)
            {
                SetTime(timeToWait);
                StartTimer();
            }
        }

        private void Update()
        {
            Tick();
        }

        public bool Tick()
        {
            if (!_isTimerRunning) return true;

            _timeSinceLastSecond += Time.deltaTime;

            if (_timeSinceLastSecond >= 1f)
            {
                _timeSinceLastSecond = 0f;

                int currentSecond = Mathf.RoundToInt(CurrentTimeLeftInSeconds);
                OnSecondReducedEvent?.Invoke(currentSecond);

                if (currentSecond <= warningPeriod)
                {
                    OnWarningSecondReducedEvent?.Invoke(currentSecond);
                }
            }

            CurrentTimeLeftInSeconds -= Time.deltaTime;
            if (CurrentTimeLeftInSeconds <= 0f)
            {
                CurrentTimeLeftInSeconds = 0f;
                _isTimerRunning = false;
                OnSecondReducedEvent?.Invoke(0);
                OnTimerEnd?.Invoke();
                return true;
            }

            return false;
        }

        public void StartTimer()
        {
            _isTimerRunning = true;
            OnTimerStart?.Invoke();
        }

        public void PauseTimer()
        {
            _wasRunningBeforePause = _isTimerRunning;
            _isTimerRunning = false;
        }

        public void ResumeTimer()
        {
            _isTimerRunning = true;
        }

        [Button]
        public void EndTimer()
        {
            CurrentTimeLeftInSeconds = 0f;

            // When the timer is paused, we have to manually invoke the events
            if (!_isTimerRunning)
            {
                OnSecondReducedEvent?.Invoke(0);
                OnTimerEnd?.Invoke();
            }
        }

        public void SetTime(float time)
        {
            timeToWait = time;
            CurrentTimeLeftInSeconds = time;
            OnSecondReducedEvent?.Invoke(Mathf.RoundToInt(time));
        }

        public void AddToCurrentTime(float time)
        {
            CurrentTimeLeftInSeconds += time;
        }
    }
}