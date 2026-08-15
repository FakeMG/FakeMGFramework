namespace FakeMG.SaveLoad
{
    /// <summary>
    /// Owns the monotonic automatic-save countdown independently from MonoBehaviour update and save
    /// execution. Configuration changes reset the next due time predictably.
    /// </summary>
    public sealed class AutoSaveSchedule
    {
        private float _intervalSeconds;
        private float _remainingSeconds;

        public AutoSaveSchedule(float intervalSeconds)
        {
            Reset(intervalSeconds);
        }

        #region Public Methods

        public bool Advance(float elapsedSeconds)
        {
            _remainingSeconds -= elapsedSeconds;
            if (_remainingSeconds > 0f)
            {
                return false;
            }

            _remainingSeconds = _intervalSeconds;
            return true;
        }

        public void Reset(float intervalSeconds)
        {
            _intervalSeconds = intervalSeconds;
            _remainingSeconds = intervalSeconds;
        }

        #endregion
    }
}
