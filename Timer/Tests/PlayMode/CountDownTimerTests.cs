using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace FakeMG.FakeMGFramework.Timer.Tests {
    public class CountDownTimerTests {
        private GameObject _timerGameObject;
        private CountDownTimer _countDownTimer;

        [SetUp]
        public void Setup() {
            _timerGameObject = new GameObject("TimerTestObject");
            _countDownTimer = _timerGameObject.AddComponent<CountDownTimer>();
            //init all the unity events
            _countDownTimer.onSecondReducedEvent = new UnityEvent<int>();
            _countDownTimer.onTimerEndEvent = new UnityEvent();
            _countDownTimer.onWarningSecondReducedEvent = new UnityEvent<int>();

            Time.timeScale = 4f;
        }

        [TearDown]
        public void TearDown() {
            Object.Destroy(_timerGameObject);
        }

        [UnityTest]
        public IEnumerator SetTimeToWait_InitializesTimer() {
            // Arrange
            const float testTime = 10f;
            bool secondEventTriggered = false;

            _countDownTimer.onSecondReducedEvent.AddListener(seconds => { secondEventTriggered = true; });

            // Act
            _countDownTimer.SetTimeToWait(testTime);

            // Wait a frame to ensure events are processed
            yield return null;

            // Assert
            Assert.AreEqual(testTime, _countDownTimer.TotalTime, "TotalTime should match set time");
            Assert.AreEqual(testTime, _countDownTimer.CurrentTimeLeftInSeconds,
                "CurrentTimeInSeconds should match set time");
            Assert.IsFalse(_countDownTimer.Running, "Timer should not be running after SetTimeToWait");
            Assert.IsTrue(secondEventTriggered, "Second reduced event should trigger on SetTimeToWait");
        }

        [UnityTest]
        public IEnumerator RunsCorrectly_WhenStarted() {
            // Arrange
            const float testTime = 2f;
            int secondEventCallCount = 0;
            bool timerEndEventTriggered = false;

            _countDownTimer.onSecondReducedEvent.AddListener(seconds => { secondEventCallCount++; });

            _countDownTimer.onTimerEndEvent.AddListener(() => { timerEndEventTriggered = true; });

            // Act
            _countDownTimer.SetTimeToWait(testTime);
            _countDownTimer.SetRunning(true);

            // Wait enough time for the timer to expire (add a little buffer)
            yield return new WaitForSeconds(testTime + 0.5f);

            // Assert
            Assert.IsFalse(_countDownTimer.Running, "Timer should stop running after completion");
            Assert.IsTrue(timerEndEventTriggered, "Timer end event should have triggered");
            Assert.GreaterOrEqual(secondEventCallCount, 2, "Should have called second reduced event at least twice");
        }

        [UnityTest]
        public IEnumerator WarningEvents_TriggerCorrectly() {
            // Arrange
            const float testTime = 7f; // Time longer than warning period
            const float warningPeriod = 5f;
            int warningEventCallCount = 0;

            // Explicitly access and set the field using reflection to override serialized value
            var warningPeriodField = typeof(CountDownTimer).GetField("warningPeriod",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            warningPeriodField.SetValue(_countDownTimer, warningPeriod);

            _countDownTimer.onWarningSecondReducedEvent.AddListener(seconds => { warningEventCallCount++; });

            // Act
            _countDownTimer.SetTimeToWait(testTime);
            _countDownTimer.SetRunning(true);

            // Wait until we enter the warning period plus some buffer
            yield return new WaitForSeconds(testTime - warningPeriod + 0.5f);

            // Assert warnings are being triggered
            Assert.Greater(warningEventCallCount, 0, "Warning events should have started triggering");

            // Continue until timer completes
            yield return new WaitForSeconds(warningPeriod + 0.5f);

            // Assert
            Assert.AreEqual(warningPeriod, _countDownTimer.WarningPeriod, "Warning period was not set correctly");
            Assert.AreEqual(Mathf.RoundToInt(warningPeriod), warningEventCallCount,
                "Warning event should trigger once per second during warning period");
        }

        [UnityTest]
        public IEnumerator AddTime_ExtendsCurrentTime() {
            // Arrange
            const float initialTime = 2f;
            const float additionalTime = 3f;
            bool timerEndEventTriggered = false;

            yield return null;

            _countDownTimer.onTimerEndEvent.AddListener(() => { timerEndEventTriggered = true; });

            // Act
            _countDownTimer.SetTimeToWait(initialTime);
            _countDownTimer.SetRunning(true);

            // Wait a bit then add time
            yield return new WaitForSeconds(1f);
            _countDownTimer.AddTime(additionalTime);

            // Timer should now have about 4 seconds left
            // Wait a little less to check it's still running
            yield return new WaitForSeconds(3.5f);

            // Assert
            Assert.IsTrue(_countDownTimer.Running, "Timer should still be running after adding time");
            Assert.IsFalse(timerEndEventTriggered, "Timer end event should not have triggered yet");

            // Wait a bit more to let it finish
            yield return new WaitForSeconds(1f);

            // Assert
            Assert.IsFalse(_countDownTimer.Running, "Timer should stop running after completion");
            Assert.IsTrue(timerEndEventTriggered, "Timer end event should have triggered");
        }
    }
}