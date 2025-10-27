using NUnit.Framework;

namespace Timer.EditMode
{
    public class TimerTests
    {
        [Test]
        public void Initialization_SetsCorrectValues()
        {
            // Arrange & Act
            const float EXPECTED_TIME = 10f;
            var timer = new FakeMG.Framework.Timer.Timer(EXPECTED_TIME);

            // Assert
            Assert.AreEqual(EXPECTED_TIME, timer.TimeToWait, "TimeToWait should be initialized with constructor value");
            Assert.AreEqual(EXPECTED_TIME, timer.CurrentTimeLeftInSeconds,
                "CurrentTimeInSeconds should be initialized with constructor value");
        }

        [Test]
        public void Tick_DecreasesTimeCorrectly()
        {
            // Arrange
            const float START_TIME = 5f;
            const float DELTA_TIME = 1f;
            var timer = new FakeMG.Framework.Timer.Timer(START_TIME);

            // Act
            bool isFinished = timer.Tick(DELTA_TIME);

            // Assert
            Assert.AreEqual(START_TIME - DELTA_TIME, timer.CurrentTimeLeftInSeconds,
                "Current time should decrease by delta time");
            Assert.IsFalse(isFinished, "Timer should not be finished after one tick");
        }

        [Test]
        public void Tick_Finished_ReturnsTrue()
        {
            // Arrange
            const float START_TIME = 2f;
            var timer = new FakeMG.Framework.Timer.Timer(START_TIME);

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
        public void SetTime_ResetsTimer()
        {
            // Arrange
            var timer = new FakeMG.Framework.Timer.Timer(10f);
            timer.Tick(5f); // Timer at 5 seconds

            // Act
            const float NEW_TIME = 20f;
            timer.SetTime(NEW_TIME);

            // Assert
            Assert.AreEqual(NEW_TIME, timer.TimeToWait, "TimeToWait should be updated");
            Assert.AreEqual(NEW_TIME, timer.CurrentTimeLeftInSeconds,
                "CurrentTimeInSeconds should be reset to new time");
        }

        [Test]
        public void AddTime_PositiveValue_IncreasesCurrentTime()
        {
            // Arrange
            const float START_TIME = 10f;
            var timer = new FakeMG.Framework.Timer.Timer(START_TIME);
            timer.Tick(5f); // Timer at 5 seconds

            // Act
            const float TIME_TO_ADD = 3f;
            timer.AddTime(TIME_TO_ADD);

            // Assert
            Assert.AreEqual(START_TIME, timer.TimeToWait, "TimeToWait should remain unchanged");
            Assert.AreEqual(8f, timer.CurrentTimeLeftInSeconds, "CurrentTimeInSeconds should increase by added time");
        }
    }
}