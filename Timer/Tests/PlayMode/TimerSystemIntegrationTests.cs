using System.Collections;
using System.Reflection;
using FakeMG.Framework.Timer;
using NUnit.Framework;
using TMPro;
using UnityEngine;
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
        private readonly WaitForSeconds _waitForSeconds2 = new(2f);

        [SetUp]
        public void Setup()
        {
            // Set up CountDownTimer
            _timerGameObject = new GameObject("TimerTestObject");
            _countDownTimer = _timerGameObject.AddComponent<CountDownTimer>();

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
            const float TEST_TIME_IN_SECOND = 10f;
            string initialText = "";
            string updatedText = "";

            // Connect the CountDownTimer to the TimerTextUIUpdater
            _countDownTimer.OnSecondReducedEvent += _timerTextUpdater.UpdateUI;

            // Act
            _countDownTimer.SetTime(TEST_TIME_IN_SECOND);
            initialText = _timerText.text;

            _countDownTimer.StartTimer();
            yield return new WaitUntil(() => _countDownTimer.CurrentTimeLeftInSeconds <= 8f);
            updatedText = _timerText.text;

            // Assert
            Assert.AreEqual("00:10", initialText, "Initial timer display should show 00:10");
            Assert.AreEqual("00:08", updatedText, "Timer display should update after 2 seconds");
        }

        [UnityTest]
        public IEnumerator AutoStart_InitializesCorrectly()
        {
            // Readding CountDownTimer to ensure fresh state
            Object.Destroy(_countDownTimer);
            _countDownTimer = _timerGameObject.AddComponent<CountDownTimer>();

            // Set properties using reflection since they're serialized private fields
            var playOnStartField = typeof(CountDownTimer).GetField("playOnStart",
                BindingFlags.NonPublic | BindingFlags.Instance);
            playOnStartField.SetValue(_countDownTimer, true);

            var timeToWaitField = typeof(CountDownTimer).GetField("timeToWait",
                BindingFlags.NonPublic | BindingFlags.Instance);
            timeToWaitField.SetValue(_countDownTimer, 5f);

            // Connect UI updater
            _countDownTimer.OnSecondReducedEvent += _timerTextUpdater.UpdateUI;

            // Let Start() run
            yield return null;

            // Assert
            Assert.IsTrue(_countDownTimer.IsTimerRunning, "Timer should be running after auto-start");
            // For some reason the timing is slightly off. Maybe due the yield return null above.
            Assert.That(_countDownTimer.CurrentTimeLeftInSeconds, Is.EqualTo(5f).Within(0.02f), "Timer should have correct initial time");
            Assert.AreEqual("00:05", _timerText.text, "UI should display initial time");

            // Let it run a bit
            yield return _waitForSeconds2;

            // Assert the time decreases
            Assert.Less(_countDownTimer.CurrentTimeLeftInSeconds, 4f, "Time should decrease");
            Assert.AreEqual("00:03", _timerText.text, "UI should update with current time");
        }
    }
}