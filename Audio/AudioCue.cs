using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using static FakeMG.Framework.Audio.AudioConfigurationSO;

namespace FakeMG.Framework.Audio
{
    public class AudioCue : MonoBehaviour
    {
        [Header("Sound definition")]
        [SerializeField] private AudioCueSO audioCue;
        [SerializeField] private bool playOnStart;
        [SerializeField] private float startDelay;
        [SerializeField] private bool stopOnDisable;

        [Header("Event Channel")]
        [SerializeField] private AudioCueEventChannelSO audioCueEventChannel;

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Use predefined AudioConfigurationSO or define custom settings")]
        private bool usePredefinedConfiguration = true;

        [SerializeField]
        [ShowIf(nameof(usePredefinedConfiguration))]
        [InlineEditor]
        private AudioConfigurationSO audioConfiguration;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        private AudioMixerGroup outputAudioMixerGroup;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        [EnumToggleButtons]
        private PriorityLevel priorityLevel = PriorityLevel.Standard;

        [Title("Sound Properties")]
        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        private bool mute;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        [Range(0f, 1f)]
        private float volume = 1f;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        [Range(-3f, 3f)]
        private float pitch = 1f;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        [Range(-1f, 1f)]
        private float panStereo;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        [Range(0f, 1.1f)]
        private float reverbZoneMix = 1f;

        [Title("Spatialization")]
        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        [Range(0f, 1f)]
        private float spatialBlend = 1f;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        private AudioRolloffMode rollOffMode = AudioRolloffMode.Logarithmic;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        [MinValue(0f)]
        [OnValueChanged(nameof(ValidateDistances))]
        private float minDistance = 0.1f;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        [MinValue(0.01f)]
        [OnValueChanged(nameof(ValidateDistances))]
        private float maxDistance = 50f;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        [Range(0, 360)]
        private int spread;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        [Range(0f, 5f)]
        private float dopplerLevel = 1f;

        [Title("Ignores")]
        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        private bool bypassEffects;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        private bool bypassListenerEffects;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        private bool bypassReverbZones;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        private bool ignoreListenerVolume;

        [SerializeField]
        [HideIf(nameof(usePredefinedConfiguration))]
        [BoxGroup("Custom Audio Settings")]
        private bool ignoreListenerPause;

        [SerializeField] private bool followParent;

        private AudioCueKey _controlKey = AudioCueKey.Invalid;

        private void Start()
        {
            if (playOnStart)
            {
                StartCoroutine(PlayDelayed());
            }
        }

        private void OnDisable()
        {
            if (stopOnDisable)
            {
                StopAudioCue();
            }
        }

        private IEnumerator PlayDelayed()
        {
            yield return new WaitForSeconds(startDelay);
            PlayAudioCue();
        }

        public void PlayAudioCue()
        {
            if (audioCue.CanPlaySound())
            {
                var transformToUse = followParent ? transform : null;
                var audioConfigToUse = GetAudioConfiguration();
                _controlKey =
                    audioCueEventChannel.RaisePlayEvent(audioCue, audioConfigToUse, transform.position, transformToUse);
                audioCue.UpdateLastPlayTime();
            }
        }

        private AudioConfigurationSO GetAudioConfiguration()
        {
            if (usePredefinedConfiguration)
            {
                return audioConfiguration;
            }

            // Create a runtime instance with custom settings
            var customConfig = ScriptableObject.CreateInstance<AudioConfigurationSO>();
            customConfig.outputAudioMixerGroup = outputAudioMixerGroup;
            customConfig.Priority = (int)priorityLevel;
            customConfig.mute = mute;
            customConfig.volume = volume;
            customConfig.pitch = pitch;
            customConfig.panStereo = panStereo;
            customConfig.reverbZoneMix = reverbZoneMix;
            customConfig.spatialBlend = spatialBlend;
            customConfig.rollOffMode = rollOffMode;
            customConfig.minDistance = minDistance;
            customConfig.maxDistance = maxDistance;
            customConfig.spread = spread;
            customConfig.dopplerLevel = dopplerLevel;
            customConfig.bypassEffects = bypassEffects;
            customConfig.bypassListenerEffects = bypassListenerEffects;
            customConfig.bypassReverbZones = bypassReverbZones;
            customConfig.ignoreListenerVolume = ignoreListenerVolume;
            customConfig.ignoreListenerPause = ignoreListenerPause;

            return customConfig;
        }

        public void StopAudioCue()
        {
            if (_controlKey != AudioCueKey.Invalid)
            {
                if (!audioCueEventChannel.RaiseStopEvent(_controlKey, audioCue))
                {
                    _controlKey = AudioCueKey.Invalid;
                }
            }
        }

        public void FinishAudioCue()
        {
            if (_controlKey != AudioCueKey.Invalid)
            {
                if (!audioCueEventChannel.RaiseFinishEvent(_controlKey))
                {
                    _controlKey = AudioCueKey.Invalid;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Only draw if using custom configuration or if we have a predefined config to reference
            if (!usePredefinedConfiguration)
            {
                DrawAudioRangeGizmos(minDistance, maxDistance);
            }
            else if (audioConfiguration != null)
            {
                DrawAudioRangeGizmos(audioConfiguration.minDistance, audioConfiguration.maxDistance);
            }
        }

        private void DrawAudioRangeGizmos(float minDist, float maxDist)
        {
            Vector3 position = transform.position;

            // Draw min distance sphere in green
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(position, minDist);

            // Draw max distance sphere in red
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireSphere(position, maxDist);
        }

        private void ValidateDistances()
        {
            if (minDistance > maxDistance)
            {
                maxDistance = minDistance;
            }
        }
    }
}