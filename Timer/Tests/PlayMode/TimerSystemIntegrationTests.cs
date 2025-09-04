using System.Collections;
using System.Reflection;
using FakeMG.Framework.Timer;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

namespace Timer.PlayMode
{
    public class TimerSystemIntegrationTests
    {
        private GameObject _timerGameObject;
        private GameObject _textGameObject;
        private CountDownTimer _countDownTimer;
        private TimerTextUIUpdater _timerTextUpdater;
        private TextMeshProUGUI _timerText;

        [SetUp]
        public void Setup()
        {
            // Set up CountDownTimer
            _timerGameObject = new GameObject("TimerTestObject");
            _countDownTimer = _timerGameObject.AddComponent<CountDownTimer>();

            _countDownTimer.onSecondReducedEvent = new UnityEvent<int>();
            _countDownTimer.onTimerEndEvent = new UnityEvent();
            _countDownTimer.onWarningSecondReducedEvent = new UnityEvent<int>();

            // Set up TimerTextUIUpdater
            _textGameObject = new GameObject("TextTestObject");
            _timerText = _textGameObject.AddComponent<TextMeshProUGUI>();
            _timerTextUpdater = _textGameObject.AddComponent<TimerTextUIUpdater>();

            // Use reflection to set the private timerText field
            var timerTextField = typeof(TimerTextUIUpdater).GetField("timerText",
                BindingFlags.NonPublic | BindingFlags.Instance);
            timerTextField.SetValue(_timerTextUpdater, _timerText);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_timerGameObject);
            Object.Destroy(_textGameObject);
        }

        [UnityTest]
        public IEnumerator Integration_ConnectsAndUpdatesUI()
        {
            // Arrange
            const float testTimeInSecond = 10f;
            string initialText = "";
            string updatedText = "";

            // Connect the CountDownTimer to the TimerTextUIUpdater
            _countDownTimer.onSecondReducedEvent.AddListener(_timerTextUpdater.UpdateUI);

            // Act
            _countDownTimer.SetTimeToWait(testTimeInSecond);
            initialText = _timerText.text;

            _countDownTimer.SetRunning(true);
            yield return new WaitUntil(() => _countDownTimer.CurrentTimeLeftInSeconds <= 8f);
            updatedText = _timerText.text;

            // Assert
            Assert.AreEqual("00:10", initialText, "Initial timer display should show 00:10");
            Assert.AreEqual("00:08", updatedText, "Timer display should update after 2 seconds");
        }

        [UnityTest]
        public IEnumerator AutoStart_InitializesCorrectly()
        {
            Object.Destroy(_countDownTimer);
            _countDownTimer = _timerGameObject.AddComponent<CountDownTimer>();
            _countDownTimer.onSecondReducedEvent = new UnityEvent<int>();
            _countDownTimer.onTimerEndEvent = new UnityEvent();
            _countDownTimer.onWarningSecondReducedEvent = new UnityEvent<int>();

            // Set properties using reflection since they're serialized private fields
            var runOnStartField = typeof(CountDownTimer).GetField("runOnStart",
                BindingFlags.NonPublic | BindingFlags.Instance);
            runOnStartField.SetValue(_countDownTimer, true);

            _countDownTimer.SetTimeToWait(5f);

            // Connect UI updater
            _countDownTimer.onSecondReducedEvent.AddListener(_timerTextUpdater.UpdateUI);

            // Let Start() run
            yield return null;

            // Assert
            Assert.IsTrue(_countDownTimer.Running, "Timer should be running after auto-start");
            Assert.AreEqual(5f, _countDownTimer.TotalTime, "Timer should have correct total time");
            Assert.AreEqual("00:05", _timerText.text, "UI should display initial time");

            // Let it run a bit
            yield return new WaitForSeconds(2f);

            // Assert the time decreases
            Assert.Less(_countDownTimer.CurrentTimeLeftInSeconds, 5f, "Time should decrease");
            Assert.AreEqual("00:03", _timerText.text, "UI should update with current time");
        }
    }
}