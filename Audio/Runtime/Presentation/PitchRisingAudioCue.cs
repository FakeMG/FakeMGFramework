using UnityEngine;

namespace FakeMG.Audio
{
    [DisallowMultipleComponent]
    public sealed class PitchRisingAudioCue : MonoBehaviour
    {
        [Header("Playback")]
        [SerializeField] private AudioCue _audioCue;

        [Header("Pitch Streak")]
        [Tooltip("Pitch multiplier added for each sound played within the reset window.")]
        [Min(0.001f), SerializeField] private float _pitchStepMultiplier = 0.06f;
        [Range(1f, 3f), SerializeField] private float _maximumPitchMultiplier = 1.5f;
        [Tooltip("Silence duration before pitch returns to its base value. Uses unscaled time for UI.")]
        [Min(0.01f), SerializeField] private float _resetAfterSeconds = 0.6f;

        private AudioPitchStreak _pitchStreak;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _pitchStreak = new AudioPitchStreak(_pitchStepMultiplier, _maximumPitchMultiplier, _resetAfterSeconds);
        }

        #endregion

        #region Public Methods

        public void PlayAudioCue()
        {
            float currentTimeSeconds = Time.unscaledTime;
            float pitchMultiplier = _pitchStreak.GetNextPitchMultiplier(currentTimeSeconds);
            if (_audioCue.TryPlayAudioCue(pitchMultiplier))
            {
                _pitchStreak.RecordPlayback(currentTimeSeconds);
            }
        }

        #endregion
    }
}
