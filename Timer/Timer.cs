namespace FakeMG.Timer {
    public class Timer {
        public float TimeToWait { get; private set; }
        public float CurrentTimeLeftInSeconds { get; private set; }

        public Timer(float timeToWait) {
            SetTime(timeToWait);
        }

        public bool Tick(float deltaTime) {
            CurrentTimeLeftInSeconds -= deltaTime;
            if (CurrentTimeLeftInSeconds <= 0f) {
                CurrentTimeLeftInSeconds = 0f;
                return true; // Timer has finished
            }

            return false; // Timer is still running
        }

        public void SetTime(float time) {
            TimeToWait = time;
            CurrentTimeLeftInSeconds = time;
        }

        public void AddTime(float time) {
            CurrentTimeLeftInSeconds += time;
        }
    }
}