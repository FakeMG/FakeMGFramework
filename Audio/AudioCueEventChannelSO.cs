using FakeMG.Framework;
using FakeMG.Settings;
using UnityEngine;
using UnityEngine.Audio;

namespace FakeMG.Audio
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.AUDIO + "/AudioCueEventChannel")]
    public class AudioCueEventChannelSO : ScriptableObject
    {
        [Header("Output")]
        [SerializeField]
        private AudioMixerGroup _outputAudioMixerGroup;

        [Header("Volume")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private string _mixerVolumeParameter;
        [SerializeField] private SliderSettingSO _volumeSliderSetting;

        public event AudioCuePlayAction OnAudioCuePlayRequested;
        public event AudioCueStopAction OnAudioCueStopRequested;
        public event AudioCueFinishAction OnAudioCueFinishRequested;

        public SliderSettingSO VolumeSliderSetting => _volumeSliderSetting;
        public AudioMixer AudioMixer => _audioMixer;
        public string MixerVolumeParameter => _mixerVolumeParameter;
        public bool HasVolumeConfiguration => _audioMixer && !string.IsNullOrWhiteSpace(_mixerVolumeParameter);

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