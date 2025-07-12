using FakeMG.FakeMGFramework.Timer;
using NUnit.Framework;
using TMPro;
using UnityEngine;

namespace Timer.PlayMode
{
    public class TimerTextUIUpdaterTests
    {
        private GameObject _textGameObject;
        private TextMeshProUGUI _timerText;
        private TimerTextUIUpdater _timerTextUpdater;

        [SetUp]
        public void Setup()
        {
            _textGameObject = new GameObject("TextTestObject");
            _timerText = _textGameObject.AddComponent<TextMeshProUGUI>();
            _timerTextUpdater = _textGameObject.AddComponent<TimerTextUIUpdater>();

            // Use reflection to set the private timerText field
            var timerTextField = typeof(TimerTextUIUpdater).GetField("timerText",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            timerTextField.SetValue(_timerTextUpdater, _timerText);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_textGameObject);
        }

        [Test]
        public void UpdateUI_FormatsTimeCorrectly()
        {
            // Test case 1: zero seconds
            _timerTextUpdater.UpdateUI(0);
            Assert.AreEqual("00:00", _timerText.text, "Should display 00:00 for zero seconds");

            // Test case 2: only seconds (less than a minute)
            _timerTextUpdater.UpdateUI(45);
            Assert.AreEqual("00:45", _timerText.text, "Should display 00:45 for 45 seconds");

            // Test case 3: minutes and seconds
            _timerTextUpdater.UpdateUI(125); // 2 minutes and 5 seconds
            Assert.AreEqual("02:05", _timerText.text, "Should display 02:05 for 125 seconds");

            // Test case 4: single digit minutes and seconds
            _timerTextUpdater.UpdateUI(65); // 1 minute and 5 seconds
            Assert.AreEqual("01:05", _timerText.text, "Should display 01:05 for 65 seconds");

            // Test case 5: large value
            _timerTextUpdater.UpdateUI(3661); // 61 minutes and 1 second
            Assert.AreEqual("61:01", _timerText.text, "Should display 61:01 for 3661 seconds");
        }

        [Test]
        public void UpdateUI_HandlesNegativeValues()
        {
            // Timer should display 00:00 for negative values
            _timerTextUpdater.UpdateUI(-10);
            Assert.AreEqual("00:00", _timerText.text, "Should display 00:00 for negative seconds");
        }
    }
}