using System;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.Audio;

namespace FakeMG.Audio
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.AUDIO + "/AudioCueEventChannel")]
    public class AudioCueEventChannelSO : ScriptableObject
    {
        private const float MIN_LINEAR_VOLUME = 0.0001f;
        private const float DECIBEL_SCALE = 20f;
        private const float ENABLED_VOLUME = 1f;
        private const float DISABLED_VOLUME = 0f;

        [Header("Output")]
        [SerializeField]
        private AudioMixerGroup _outputAudioMixerGroup;

        [Header("Volume")]
        [SerializeField] private string _channelId;
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private string _mixerVolumeParameter;
        [SerializeField] private string _playerPrefsKey;
        [SerializeField][Range(0f, 1f)] private float _defaultVolume = 1f;

        public event AudioCuePlayAction OnAudioCuePlayRequested;
        public event AudioCueStopAction OnAudioCueStopRequested;
        public event AudioCueFinishAction OnAudioCueFinishRequested;

        public AudioCueKey RaisePlayEvent(
            AudioCueSO audioCue,
            AudioConfigurationSO audioConfiguration,
            Vector3 positionInSpace = default,
            Transform parent = null)
        {
            AudioCueKey audioCueKey = AudioCueKey.Invalid;

            if (OnAudioCuePlayRequested != null)
            {
                audioCueKey = OnAudioCuePlayRequested.Invoke(
                    audioCue,
                    audioConfiguration,
                    _outputAudioMixerGroup,
                    positionInSpace,
                    parent);
            }
            else
            {
                Debug.LogWarning("An AudioCue play event was requested  for " + audioCue.name +
                                 ", but nobody picked it up. " +
                                 "Check why there is no AudioManager already loaded, " +
                                 "and make sure it's listening on this AudioCue Event channel.");
            }

            return audioCueKey;
        }

        public bool RaiseStopEvent(AudioCueKey audioCueKey, AudioCueSO audioCue)
        {
            bool requestSucceed = false;

            if (OnAudioCueStopRequested != null)
            {
                requestSucceed = OnAudioCueStopRequested.Invoke(audioCueKey, audioCue);
            }
            else
            {
                Debug.LogWarning("An AudioCue stop event was requested, but nobody picked it up. " +
                                 "Check why there is no AudioManager already loaded, " +
                                 "and make sure it's listening on this AudioCue Event channel.");
            }

            return requestSucceed;
        }

        public bool RaiseFinishEvent(AudioCueKey audioCueKey)
        {
            bool requestSucceed = false;

            if (OnAudioCueFinishRequested != null)
            {
                requestSucceed = OnAudioCueFinishRequested.Invoke(audioCueKey);
            }
            else
            {
                Debug.LogWarning("An AudioCue finish event was requested, but nobody picked it up. " +
                                 "Check why there is no AudioManager already loaded, " +
                                 "and make sure it's listening on this AudioCue Event channel.");
            }

            return requestSucceed;
        }

        public bool MatchesChannel(string channelId)
        {
            return string.Equals(_channelId, channelId, StringComparison.Ordinal);
        }

        public void InitializePersistedVolume()
        {
            if (!HasVolumeConfiguration())
            {
                return;
            }

            float persistedVolume = PlayerPrefs.GetFloat(_playerPrefsKey, Mathf.Clamp01(_defaultVolume));
            ApplyVolumeChange(persistedVolume);
        }

        public void SetVolume(float volume)
        {
            if (!HasVolumeConfiguration())
            {
                Debug.LogWarning($"Volume change was ignored because channel '{_channelId}' is not configured.", this);
                return;
            }

            ApplyVolumeChange(volume);
        }

        public void SetChannelEnabled(bool isEnabled)
        {
            float volume = isEnabled ? ENABLED_VOLUME : DISABLED_VOLUME;
            SetVolume(volume);
        }

        private bool HasVolumeConfiguration()
        {
            return !string.IsNullOrWhiteSpace(_channelId) &&
                   _audioMixer &&
                   !string.IsNullOrWhiteSpace(_mixerVolumeParameter) &&
                   !string.IsNullOrWhiteSpace(_playerPrefsKey);
        }

        private void ApplyVolumeChange(float newVolume)
        {
            float clampedVolume = Mathf.Clamp01(newVolume);
            float stableLinearVolume = Mathf.Max(clampedVolume, MIN_LINEAR_VOLUME);
            float decibelValue = Mathf.Log10(stableLinearVolume) * DECIBEL_SCALE;

            bool volumeSet = _audioMixer.SetFloat(_mixerVolumeParameter, decibelValue);
            if (!volumeSet)
            {
                Debug.LogError($"The AudioMixer parameter '{_mixerVolumeParameter}' was not found.", this);
            }

            PlayerPrefs.SetFloat(_playerPrefsKey, clampedVolume);
            PlayerPrefs.Save();
        }
    }

    public delegate AudioCueKey AudioCuePlayAction(
        AudioCueSO audioCue,
        AudioConfigurationSO audioConfiguration,
        AudioMixerGroup outputAudioMixerGroup,
        Vector3 positionInSpace,
        Transform parent);

    public delegate bool AudioCueStopAction(AudioCueKey emitterKey, AudioCueSO audioCue);

    public delegate bool AudioCueFinishAction(AudioCueKey emitterKey);
}