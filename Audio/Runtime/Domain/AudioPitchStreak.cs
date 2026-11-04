using System;

namespace FakeMG.Audio
{
    public sealed class AudioPitchStreak
    {
        private readonly float _pitchStepMultiplier;
        private readonly float _maximumPitchMultiplier;
        private readonly float _resetAfterSeconds;

        private float _lastPlaybackTimeSeconds = float.NegativeInfinity;
        private float _lastPitchMultiplier = 1f;

        public AudioPitchStreak(float pitchStepMultiplier, float maximumPitchMultiplier, float resetAfterSeconds)
        {
            if (!float.IsFinite(pitchStepMultiplier) || pitchStepMultiplier <= 0f)
                throw new ArgumentOutOfRangeException(nameof(pitchStepMultiplier));
            if (!float.IsFinite(maximumPitchMultiplier) || maximumPitchMultiplier < 1f || maximumPitchMultiplier > 3f)
                throw new ArgumentOutOfRangeException(nameof(maximumPitchMultiplier));
            if (!float.IsFinite(resetAfterSeconds) || resetAfterSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(resetAfterSeconds));

            _pitchStepMultiplier = pitchStepMultiplier;
            _maximumPitchMultiplier = maximumPitchMultiplier;
            _resetAfterSeconds = resetAfterSeconds;
        }

        #region Public Methods

        public float GetNextPitchMultiplier(float currentTimeSeconds)
        {
            float elapsedSeconds = currentTimeSeconds - _lastPlaybackTimeSeconds;
            return elapsedSeconds < 0f || elapsedSeconds >= _resetAfterSeconds
                ? 1f
                : Math.Min(_lastPitchMultiplier + _pitchStepMultiplier, _maximumPitchMultiplier);
        }

        public void RecordPlayback(float currentTimeSeconds)
        {
            _lastPitchMultiplier = GetNextPitchMultiplier(currentTimeSeconds);
            _lastPlaybackTimeSeconds = currentTimeSeconds;
        }

        #endregion
    }
}
