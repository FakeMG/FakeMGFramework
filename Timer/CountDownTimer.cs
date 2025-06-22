using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.FakeMGFramework.Timer
{
    public class CountDownTimer : MonoBehaviour
    {
        [SerializeField] private bool runOnStart;
        [SerializeField] private float timeToWait;
        [SerializeField] private float warningPeriod = 5f;

        public UnityEvent onTimerEndEvent;
        public UnityEvent<int> onSecondReducedEvent;
        public UnityEvent<int> onWarningSecondReducedEvent;

        private Timer _timer;
        private float _timeSinceLastSecond;

        public float WarningPeriod => warningPeriod;
        public bool Running { get; private set; }
        public float CurrentTimeLeftInSeconds => _timer?.CurrentTimeLeftInSeconds ?? 0f;
        public float TotalTime => _timer?.TimeToWait ?? 0f;

        private void Start()
        {
            if (runOnStart)
            {
                SetTimeToWait(timeToWait);
                SetRunning(true);
            }
        }

        private void Update()
        {
            if (_timer == null || !Running)
                return;

            _timeSinceLastSecond += Time.deltaTime;

            if (_timeSinceLastSecond >= 1f)
            {
                _timeSinceLastSecond = 0f;

                int currentSecond = Mathf.RoundToInt(CurrentTimeLeftInSeconds);
                onSecondReducedEvent.Invoke(currentSecond);

                if (currentSecond <= warningPeriod)
                {
                    onWarningSecondReducedEvent.Invoke(currentSecond);
                }
            }

            if (Running && _timer.Tick(Time.deltaTime))
            {
                Running = false;
                onTimerEndEvent.Invoke();
            }
        }

        public void SetTimeToWait(float time)
        {
            if (_timer == null)
            {
                _timer = new Timer(time);
            }
            else
            {
                _timer.SetTime(time);
            }

            timeToWait = time;
            Running = false;
            int currentSecond = Mathf.RoundToInt(time);
            onSecondReducedEvent.Invoke(currentSecond);
        }

        public void SetRunning(bool running)
        {
            Running = running;
        }

        public void AddTime(float time)
        {
            _timer?.AddTime(time);
        }
    }
}