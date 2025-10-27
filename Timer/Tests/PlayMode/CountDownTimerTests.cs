using System.Collections;
using System.Reflection;
using FakeMG.Framework.Timer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Timer.PlayMode
{
    public class CountDownTimerTests
    {
        private GameObject _timerGameObject;
        private CountDownTimer _countDownTimer;
        private readonly WaitForSeconds _waitForSeconds3_5 = new(3.5f);
        private readonly WaitForSeconds _waitForSeconds1 = new(1f);

        [SetUp]
        public void Setup()
        {
            _timerGameObject = new GameObject("TimerTestObject");
            _countDownTimer = _timerGameObject.AddComponent<CountDownTimer>();

            Time.timeScale = 4f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_timerGameObject);
        }

        [UnityTest]
        public IEnumerator SetTime_InitializesTimer()
        {
            // Arrange
            const float TEST_TIME = 10f;
            bool secondEventTriggered = false;

            _countDownTimer.OnSecondReducedEvent += seconds => { secondEventTriggered = true; };

            // Act
            _countDownTimer.SetTime(TEST_TIME);

            // Wait a frame to ensure events are processed
            yield return null;

            // Assert
            Assert.AreEqual(TEST_TIME, _countDownTimer.CurrentTimeLeftInSeconds,
                "CurrentTimeLeftInSeconds should match set time");
            Assert.IsFalse(_countDownTimer.IsTimerRunning, "Timer should not be running after SetTime");
            Assert.IsTrue(secondEventTriggered, "Second reduced event should trigger on SetTime");
        }

        [UnityTest]
        public IEnumerator RunsCorrectly_WhenStarted()
        {
            // Arrange
            const float TEST_TIME = 2f;
            int secondEventCallCount = 0;
            bool timerEndEventTriggered = false;

            _countDownTimer.OnSecondReducedEvent += seconds => { secondEventCallCount++; };
            _countDownTimer.OnTimerEnd += () => { timerEndEventTriggered = true; };

            // Act
            _countDownTimer.SetTime(TEST_TIME);
            _countDownTimer.StartTimer();

            // Wait enough time for the timer to expire (add a little buffer)
            yield return new WaitForSeconds(TEST_TIME + 0.5f);

            // Assert
            Assert.IsFalse(_countDownTimer.IsTimerRunning, "Timer should stop running after completion");
            Assert.IsTrue(timerEndEventTriggered, "Timer end event should have triggered");
            Assert.GreaterOrEqual(secondEventCallCount, 2, "Should have called second reduced event at least twice");
        }

        [UnityTest]
        public IEnumerator WarningEvents_TriggerCorrectly()
        {
            // Arrange
            const float TEST_TIME = 7f; // Time longer than warning period
            const float WARNING_PERIOD = 5f;
            int warningEventCallCount = 0;

            // Explicitly access and set the field using reflection to override serialized value
            var warningPeriodField = typeof(CountDownTimer).GetField("warningPeriod",
                BindingFlags.NonPublic | BindingFlags.Instance);
            warningPeriodField.SetValue(_countDownTimer, WARNING_PERIOD);

            _countDownTimer.OnWarningSecondReducedEvent += seconds => { warningEventCallCount++; };

            // Act
            _countDownTimer.SetTime(TEST_TIME);
            _countDownTimer.StartTimer();

            // Wait until we enter the warning period plus some buffer
            yield return new WaitForSeconds(TEST_TIME - WARNING_PERIOD + 0.5f);

            // Assert warnings are being triggered
            Assert.Greater(warningEventCallCount, 0, "Warning events should have started triggering");

            // Continue until timer completes
            yield return new WaitForSeconds(WARNING_PERIOD + 0.5f);

            // Assert
            Assert.AreEqual(Mathf.RoundToInt(WARNING_PERIOD), warningEventCallCount,
                "Warning event should trigger once per second during warning period");
        }

        [UnityTest]
        public IEnumerator AddToCurrentTime_ExtendsCurrentTime()
        {
            // Arrange
            const float INITIAL_TIME = 2f;
            const float ADDITIONAL_TIME = 3f;
            bool timerEndEventTriggered = false;

            yield return null;

            _countDownTimer.OnTimerEnd += () => { timerEndEventTriggered = true; };

            // Act
            _countDownTimer.SetTime(INITIAL_TIME);
            _countDownTimer.StartTimer();

            // Wait a bit then add time
            yield return _waitForSeconds1;
            _countDownTimer.AddToCurrentTime(ADDITIONAL_TIME);

            // Timer should now have about 4 seconds left
            // Wait a little less to check it's still running
            yield return _waitForSeconds3_5;

            // Assert
            Assert.IsTrue(_countDownTimer.IsTimerRunning, "Timer should still be running after adding time");
            Assert.IsFalse(timerEndEventTriggered, "Timer end event should not have triggered yet");

            // Wait a bit more to let it finish
            yield return _waitForSeconds1;

            // Assert
            Assert.IsFalse(_countDownTimer.IsTimerRunning, "Timer should stop running after completion");
            Assert.IsTrue(timerEndEventTriggered, "Timer end event should have triggered");
        }

        [UnityTest]
        public IEnumerator PauseTimer_StopsTimerExecution()
        {
            // Arrange
            const float TEST_TIME = 5f;
            _countDownTimer.SetTime(TEST_TIME);
            _countDownTimer.StartTimer();

            // Act
            yield return _waitForSeconds1;
            float timeBeforePause = _countDownTimer.CurrentTimeLeftInSeconds;
            _countDownTimer.PauseTimer();

            // Wait while paused
            yield return _waitForSeconds1;

            // Assert
            Assert.IsFalse(_countDownTimer.IsTimerRunning, "Timer should not be running when paused");
            Assert.IsTrue(_countDownTimer.WasRunningBeforePause, "Should remember it was running before pause");
            Assert.AreEqual(timeBeforePause, _countDownTimer.CurrentTimeLeftInSeconds, 0.1f,
                "Time should not decrease while paused");
        }

        [UnityTest]
        public IEnumerator ResumeTimer_ContinuesFromPausedState()
        {
            // Arrange
            const float TEST_TIME = 3f;
            _countDownTimer.SetTime(TEST_TIME);
            _countDownTimer.StartTimer();

            // Pause after 1 second
            yield return _waitForSeconds1;
            _countDownTimer.PauseTimer();
            float timeAfterPause = _countDownTimer.CurrentTimeLeftInSeconds;

            // Wait while paused
            yield return _waitForSeconds1;

            // Act - Resume
            _countDownTimer.ResumeTimer();
            yield return _waitForSeconds1;

            // Assert
            Assert.IsTrue(_countDownTimer.IsTimerRunning, "Timer should be running after resume");
            Assert.Less(_countDownTimer.CurrentTimeLeftInSeconds, timeAfterPause,
                "Time should decrease after resuming");
        }

        [UnityTest]
        public IEnumerator EndTimer_TriggersEventsWhenRunning()
        {
            // Arrange
            const float TEST_TIME = 5f;
            bool timerEndEventTriggered = false;
            int lastSecondValue = -1;

            _countDownTimer.OnSecondReducedEvent += seconds => { lastSecondValue = seconds; };
            _countDownTimer.OnTimerEnd += () => { timerEndEventTriggered = true; };

            _countDownTimer.SetTime(TEST_TIME);
            _countDownTimer.StartTimer();

            // Wait a bit then end manually
            yield return _waitForSeconds1;

            // Act
            _countDownTimer.EndTimer();

            // Wait a frame for events to process
            yield return null;

            // Assert
            Assert.AreEqual(0f, _countDownTimer.CurrentTimeLeftInSeconds, "Time should be 0 after ending");
            Assert.IsTrue(timerEndEventTriggered, "Timer end event should have triggered");
            Assert.AreEqual(0, lastSecondValue, "Second reduced event should have been called with 0");
        }

        [UnityTest]
        public IEnumerator EndTimer_TriggersEventsWhenPaused()
        {
            // Arrange
            const float TEST_TIME = 5f;
            bool timerEndEventTriggered = false;
            int lastSecondValue = -1;

            _countDownTimer.OnSecondReducedEvent += seconds => { lastSecondValue = seconds; };
            _countDownTimer.OnTimerEnd += () => { timerEndEventTriggered = true; };

            _countDownTimer.SetTime(TEST_TIME);
            _countDownTimer.StartTimer();

            yield return _waitForSeconds1;
            _countDownTimer.PauseTimer();

            // Act
            _countDownTimer.EndTimer();

            // Wait a frame for events to process
            yield return null;

            // Assert
            Assert.AreEqual(0f, _countDownTimer.CurrentTimeLeftInSeconds, "Time should be 0 after ending");
            Assert.IsTrue(timerEndEventTriggered, "Timer end event should have triggered even when paused");
            Assert.AreEqual(0, lastSecondValue, "Second reduced event should have been called with 0");
        }

        [UnityTest]
        public IEnumerator OnTimerStart_TriggersWhenStarted()
        {
            // Arrange
            const float TEST_TIME = 3f;
            bool timerStartEventTriggered = false;

            _countDownTimer.OnTimerStart += () => { timerStartEventTriggered = true; };

            // Act
            _countDownTimer.SetTime(TEST_TIME);
            _countDownTimer.StartTimer();

            yield return null;

            // Assert
            Assert.IsTrue(timerStartEventTriggered, "Timer start event should have triggered");
            Assert.IsTrue(_countDownTimer.IsTimerRunning, "Timer should be running");
        }
    }
}