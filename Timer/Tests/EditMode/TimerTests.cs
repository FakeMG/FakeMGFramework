using NUnit.Framework;

namespace FakeMG.FakeMGFramework.Timer.Tests {
    public class TimerTests {
        [Test]
        public void Initialization_SetsCorrectValues() {
            // Arrange & Act
            const float expectedTime = 10f;
            var timer = new Timer(expectedTime);

            // Assert
            Assert.AreEqual(expectedTime, timer.TimeToWait, "TimeToWait should be initialized with constructor value");
            Assert.AreEqual(expectedTime, timer.CurrentTimeLeftInSeconds,
                "CurrentTimeInSeconds should be initialized with constructor value");
        }

        [Test]
        public void Tick_DecreasesTimeCorrectly() {
            // Arrange
            const float startTime = 5f;
            const float deltaTime = 1f;
            var timer = new Timer(startTime);

            // Act
            bool isFinished = timer.Tick(deltaTime);

            // Assert
            Assert.AreEqual(startTime - deltaTime, timer.CurrentTimeLeftInSeconds,
                "Current time should decrease by delta time");
            Assert.IsFalse(isFinished, "Timer should not be finished after one tick");
        }

        [Test]
        public void Tick_Finished_ReturnsTrue() {
            // Arrange
            const float startTime = 2f;
            var timer = new Timer(startTime);

            // Act
            bool firstTickResult = timer.Tick(1f);
            bool secondTickResult = timer.Tick(1f);
            bool thirdTickResult = timer.Tick(1f);

            // Assert
            Assert.IsFalse(firstTickResult, "Timer should not finish after first tick");
            Assert.IsTrue(secondTickResult, "Timer should finish after second tick");
            Assert.IsTrue(thirdTickResult, "Timer should remain finished after third tick");
            Assert.AreEqual(0f, timer.CurrentTimeLeftInSeconds, "Timer should not go below zero");
        }

        [Test]
        public void SetTime_ResetsTimer() {
            // Arrange
            var timer = new Timer(10f);
            timer.Tick(5f); // Timer at 5 seconds

            // Act
            const float newTime = 20f;
            timer.SetTime(newTime);

            // Assert
            Assert.AreEqual(newTime, timer.TimeToWait, "TimeToWait should be updated");
            Assert.AreEqual(newTime, timer.CurrentTimeLeftInSeconds, "CurrentTimeInSeconds should be reset to new time");
        }

        [Test]
        public void AddTime_PositiveValue_IncreasesCurrentTime() {
            // Arrange
            const float startTime = 10f;
            var timer = new Timer(startTime);
            timer.Tick(5f); // Timer at 5 seconds

            // Act
            const float timeToAdd = 3f;
            timer.AddTime(timeToAdd);

            // Assert
            Assert.AreEqual(startTime, timer.TimeToWait, "TimeToWait should remain unchanged");
            Assert.AreEqual(8f, timer.CurrentTimeLeftInSeconds, "CurrentTimeInSeconds should increase by added time");
        }
    }
}