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
        [EnumToggleButtons]
        private ConfigurationMode configurationMode = ConfigurationMode.UsePredefined;

        [SerializeField]
        [ShowIf(nameof(ShowAudioConfiguration))]
        [InlineEditor]
        private AudioConfigurationSO audioConfiguration;

        #region Override Settings
        // Output & Priority
        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [FoldoutGroup("Override Settings")]
        [BoxGroup("Override Settings/Output & Priority")]
        private bool overrideOutputMixerGroup;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Output & Priority")]
        [Indent]
        [EnableIf(nameof(overrideOutputMixerGroup))]
        private AudioMixerGroup outputAudioMixerGroup;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Output & Priority")]
        private bool overridePriority;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Output & Priority")]
        [Indent]
        [EnableIf(nameof(overridePriority))]
        [EnumToggleButtons]
        private PriorityLevel priorityLevel = PriorityLevel.Standard;

        // Sound Properties
        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        private bool overrideMute;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        [Indent]
        [EnableIf(nameof(overrideMute))]
        private bool mute;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        private bool overrideVolume;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        [Indent]
        [EnableIf(nameof(overrideVolume))]
        [Range(0f, 1f)]
        private float volume = 1f;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        private bool overridePitch;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        [Indent]
        [EnableIf(nameof(overridePitch))]
        [Range(-3f, 3f)]
        private float pitch = 1f;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        private bool overridePanStereo;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        [Indent]
        [EnableIf(nameof(overridePanStereo))]
        [Range(-1f, 1f)]
        private float panStereo;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        private bool overrideReverbZoneMix;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        [Indent]
        [EnableIf(nameof(overrideReverbZoneMix))]
        [Range(0f, 1.1f)]
        private float reverbZoneMix = 1f;

        // Spatialization
        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool overrideSpatialBlend;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(overrideSpatialBlend))]
        [Range(0f, 1f)]
        private float spatialBlend = 1f;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool overrideRollOffMode;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(overrideRollOffMode))]
        private AudioRolloffMode rollOffMode = AudioRolloffMode.Logarithmic;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool overrideMinDistance;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(overrideMinDistance))]
        [MinValue(0f)]
        [OnValueChanged(nameof(ValidateDistances))]
        private float minDistance = 0.1f;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool overrideMaxDistance;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(overrideMaxDistance))]
        [MinValue(0.01f)]
        [OnValueChanged(nameof(ValidateDistances))]
        private float maxDistance = 50f;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool overrideSpread;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(overrideSpread))]
        [Range(0, 360)]
        private int spread;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool overrideDopplerLevel;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(overrideDopplerLevel))]
        [Range(0f, 5f)]
        private float dopplerLevel = 1f;

        // Bypass/Ignore Settings
        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        private bool overrideBypassEffects;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        [Indent]
        [EnableIf(nameof(overrideBypassEffects))]
        private bool bypassEffects;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        private bool overrideBypassListenerEffects;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        [Indent]
        [EnableIf(nameof(overrideBypassListenerEffects))]
        private bool bypassListenerEffects;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        private bool overrideBypassReverbZones;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        [Indent]
        [EnableIf(nameof(overrideBypassReverbZones))]
        private bool bypassReverbZones;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        private bool overrideIgnoreListenerVolume;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        [Indent]
        [EnableIf(nameof(overrideIgnoreListenerVolume))]
        private bool ignoreListenerVolume;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        private bool overrideIgnoreListenerPause;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        [Indent]
        [EnableIf(nameof(overrideIgnoreListenerPause))]
        private bool ignoreListenerPause;
        #endregion

        [SerializeField] private bool followParent;

        private AudioCueKey _controlKey = AudioCueKey.Invalid;

        private enum ConfigurationMode
        {
            UsePredefined,
            OverridePredefined,
            FullyCustom
        }

        private bool ShowAudioConfiguration() =>
            configurationMode == ConfigurationMode.UsePredefined || configurationMode == ConfigurationMode.OverridePredefined;

        private bool ShowOverrideSettings() =>
            configurationMode == ConfigurationMode.OverridePredefined || configurationMode == ConfigurationMode.FullyCustom;

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
            if (configurationMode == ConfigurationMode.UsePredefined)
            {
                return audioConfiguration;
            }

            if (configurationMode == ConfigurationMode.FullyCustom)
            {
                return CreateFullyCustomConfiguration();
            }

            // ConfigurationMode.OverridePredefined
            return CreateOverriddenConfiguration();
        }

        private AudioConfigurationSO CreateFullyCustomConfiguration()
        {
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

        private AudioConfigurationSO CreateOverriddenConfiguration()
        {
            if (audioConfiguration == null)
            {
                Debug.LogWarning("AudioConfiguration is null in Override mode. Using fully custom settings.", this);
                return CreateFullyCustomConfiguration();
            }

            var overriddenConfig = ScriptableObject.CreateInstance<AudioConfigurationSO>();

            // Start with base configuration values
            overriddenConfig.outputAudioMixerGroup = audioConfiguration.outputAudioMixerGroup;
            overriddenConfig.Priority = audioConfiguration.Priority;
            overriddenConfig.mute = audioConfiguration.mute;
            overriddenConfig.volume = audioConfiguration.volume;
            overriddenConfig.pitch = audioConfiguration.pitch;
            overriddenConfig.panStereo = audioConfiguration.panStereo;
            overriddenConfig.reverbZoneMix = audioConfiguration.reverbZoneMix;
            overriddenConfig.spatialBlend = audioConfiguration.spatialBlend;
            overriddenConfig.rollOffMode = audioConfiguration.rollOffMode;
            overriddenConfig.minDistance = audioConfiguration.minDistance;
            overriddenConfig.maxDistance = audioConfiguration.maxDistance;
            overriddenConfig.spread = audioConfiguration.spread;
            overriddenConfig.dopplerLevel = audioConfiguration.dopplerLevel;
            overriddenConfig.bypassEffects = audioConfiguration.bypassEffects;
            overriddenConfig.bypassListenerEffects = audioConfiguration.bypassListenerEffects;
            overriddenConfig.bypassReverbZones = audioConfiguration.bypassReverbZones;
            overriddenConfig.ignoreListenerVolume = audioConfiguration.ignoreListenerVolume;
            overriddenConfig.ignoreListenerPause = audioConfiguration.ignoreListenerPause;

            // Apply overrides where specified
            if (overrideOutputMixerGroup) overriddenConfig.outputAudioMixerGroup = outputAudioMixerGroup;
            if (overridePriority) overriddenConfig.Priority = (int)priorityLevel;
            if (overrideMute) overriddenConfig.mute = mute;
            if (overrideVolume) overriddenConfig.volume = volume;
            if (overridePitch) overriddenConfig.pitch = pitch;
            if (overridePanStereo) overriddenConfig.panStereo = panStereo;
            if (overrideReverbZoneMix) overriddenConfig.reverbZoneMix = reverbZoneMix;
            if (overrideSpatialBlend) overriddenConfig.spatialBlend = spatialBlend;
            if (overrideRollOffMode) overriddenConfig.rollOffMode = rollOffMode;
            if (overrideMinDistance) overriddenConfig.minDistance = minDistance;
            if (overrideMaxDistance) overriddenConfig.maxDistance = maxDistance;
            if (overrideSpread) overriddenConfig.spread = spread;
            if (overrideDopplerLevel) overriddenConfig.dopplerLevel = dopplerLevel;
            if (overrideBypassEffects) overriddenConfig.bypassEffects = bypassEffects;
            if (overrideBypassListenerEffects) overriddenConfig.bypassListenerEffects = bypassListenerEffects;
            if (overrideBypassReverbZones) overriddenConfig.bypassReverbZones = bypassReverbZones;
            if (overrideIgnoreListenerVolume) overriddenConfig.ignoreListenerVolume = ignoreListenerVolume;
            if (overrideIgnoreListenerPause) overriddenConfig.ignoreListenerPause = ignoreListenerPause;

            return overriddenConfig;
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
            float minDist = 0.1f;
            float maxDist = 50f;

            if (configurationMode == ConfigurationMode.UsePredefined && audioConfiguration != null)
            {
                minDist = audioConfiguration.minDistance;
                maxDist = audioConfiguration.maxDistance;
            }
            else if (configurationMode == ConfigurationMode.OverridePredefined)
            {
                if (audioConfiguration != null)
                {
                    minDist = overrideMinDistance ? minDistance : audioConfiguration.minDistance;
                    maxDist = overrideMaxDistance ? maxDistance : audioConfiguration.maxDistance;
                }
                else
                {
                    minDist = minDistance;
                    maxDist = maxDistance;
                }
            }
            else if (configurationMode == ConfigurationMode.FullyCustom)
            {
                minDist = minDistance;
                maxDist = maxDistance;
            }

            DrawAudioRangeGizmos(minDist, maxDist);
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