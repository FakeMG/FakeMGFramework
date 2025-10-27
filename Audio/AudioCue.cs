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
        [SerializeField] private AudioCueSO _audioCue;
        [SerializeField] private bool _playOnStart;
        [SerializeField] private float _startDelay;
        [SerializeField] private bool _stopOnDisable;

        [Header("Event Channel")]
        [SerializeField] private AudioCueEventChannelSO _audioCueEventChannel;

        [Header("Configuration")]
        [SerializeField]
        [EnumToggleButtons]
        private ConfigurationMode _configurationMode = ConfigurationMode.UsePredefined;

        [SerializeField]
        [ShowIf(nameof(ShowAudioConfiguration))]
        [InlineEditor]
        private AudioConfigurationSO _audioConfiguration;

        #region Override Settings
        // Output & Priority
        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [FoldoutGroup("Override Settings")]
        [BoxGroup("Override Settings/Output & Priority")]
        private bool _overrideOutputMixerGroup;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Output & Priority")]
        [Indent]
        [EnableIf(nameof(_overrideOutputMixerGroup))]
        private AudioMixerGroup _outputAudioMixerGroup;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Output & Priority")]
        private bool _overridePriority;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Output & Priority")]
        [Indent]
        [EnableIf(nameof(_overridePriority))]
        [EnumToggleButtons]
        private PriorityLevel _priorityLevel = PriorityLevel.Standard;

        // Sound Properties
        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        private bool _overrideMute;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        [Indent]
        [EnableIf(nameof(_overrideMute))]
        private bool _mute;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        private bool _overrideVolume;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        [Indent]
        [EnableIf(nameof(_overrideVolume))]
        [Range(0f, 1f)]
        private float _volume = 1f;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        private bool _overridePitch;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        [Indent]
        [EnableIf(nameof(_overridePitch))]
        [Range(-3f, 3f)]
        private float _pitch = 1f;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        private bool _overridePanStereo;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        [Indent]
        [EnableIf(nameof(_overridePanStereo))]
        [Range(-1f, 1f)]
        private float _panStereo;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        private bool _overrideReverbZoneMix;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Sound Properties")]
        [Indent]
        [EnableIf(nameof(_overrideReverbZoneMix))]
        [Range(0f, 1.1f)]
        private float _reverbZoneMix = 1f;

        // Spatialization
        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool _overrideSpatialBlend;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(_overrideSpatialBlend))]
        [Range(0f, 1f)]
        private float _spatialBlend = 1f;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool _overrideRollOffMode;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(_overrideRollOffMode))]
        private AudioRolloffMode _rollOffMode = AudioRolloffMode.Logarithmic;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool _overrideMinDistance;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(_overrideMinDistance))]
        [MinValue(0f)]
        [OnValueChanged(nameof(ValidateDistances))]
        private float _minDistance = 0.1f;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool _overrideMaxDistance;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(_overrideMaxDistance))]
        [MinValue(0.01f)]
        [OnValueChanged(nameof(ValidateDistances))]
        private float _maxDistance = 50f;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool _overrideSpread;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(_overrideSpread))]
        [Range(0, 360)]
        private int _spread;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        private bool _overrideDopplerLevel;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Spatialization")]
        [Indent]
        [EnableIf(nameof(_overrideDopplerLevel))]
        [Range(0f, 5f)]
        private float _dopplerLevel = 1f;

        // Bypass/Ignore Settings
        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        private bool _overrideBypassEffects;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        [Indent]
        [EnableIf(nameof(_overrideBypassEffects))]
        private bool _bypassEffects;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        private bool _overrideBypassListenerEffects;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        [Indent]
        [EnableIf(nameof(_overrideBypassListenerEffects))]
        private bool _bypassListenerEffects;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        private bool _overrideBypassReverbZones;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        [Indent]
        [EnableIf(nameof(_overrideBypassReverbZones))]
        private bool _bypassReverbZones;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        private bool _overrideIgnoreListenerVolume;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        [Indent]
        [EnableIf(nameof(_overrideIgnoreListenerVolume))]
        private bool _ignoreListenerVolume;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        private bool _overrideIgnoreListenerPause;

        [SerializeField]
        [ShowIf(nameof(ShowOverrideSettings))]
        [BoxGroup("Override Settings/Bypass & Ignore")]
        [Indent]
        [EnableIf(nameof(_overrideIgnoreListenerPause))]
        private bool _ignoreListenerPause;
        #endregion

        [SerializeField] private bool _followParent;

        private AudioCueKey _controlKey = AudioCueKey.Invalid;

        private enum ConfigurationMode
        {
            UsePredefined,
            OverridePredefined,
            FullyCustom
        }

        private bool ShowAudioConfiguration() =>
            _configurationMode == ConfigurationMode.UsePredefined || _configurationMode == ConfigurationMode.OverridePredefined;

        private bool ShowOverrideSettings() =>
            _configurationMode == ConfigurationMode.OverridePredefined || _configurationMode == ConfigurationMode.FullyCustom;

        private void Start()
        {
            if (_playOnStart)
            {
                StartCoroutine(PlayDelayed());
            }
        }

        private void OnDisable()
        {
            if (_stopOnDisable)
            {
                StopAudioCue();
            }
        }

        private IEnumerator PlayDelayed()
        {
            yield return new WaitForSeconds(_startDelay);
            PlayAudioCue();
        }

        public void PlayAudioCue()
        {
            if (_audioCue.CanPlaySound())
            {
                var transformToUse = _followParent ? transform : null;
                var audioConfigToUse = GetAudioConfiguration();
                _controlKey =
                    _audioCueEventChannel.RaisePlayEvent(_audioCue, audioConfigToUse, transform.position, transformToUse);
                _audioCue.UpdateLastPlayTime();
            }
        }

        private AudioConfigurationSO GetAudioConfiguration()
        {
            if (_configurationMode == ConfigurationMode.UsePredefined)
            {
                return _audioConfiguration;
            }

            if (_configurationMode == ConfigurationMode.FullyCustom)
            {
                return CreateFullyCustomConfiguration();
            }

            // ConfigurationMode.OverridePredefined
            return CreateOverriddenConfiguration();
        }

        private AudioConfigurationSO CreateFullyCustomConfiguration()
        {
            var customConfig = ScriptableObject.CreateInstance<AudioConfigurationSO>();
            customConfig.OutputAudioMixerGroup = _outputAudioMixerGroup;
            customConfig.Priority = (int)_priorityLevel;
            customConfig.Mute = _mute;
            customConfig.Volume = _volume;
            customConfig.Pitch = _pitch;
            customConfig.PanStereo = _panStereo;
            customConfig.ReverbZoneMix = _reverbZoneMix;
            customConfig.SpatialBlend = _spatialBlend;
            customConfig.RollOffMode = _rollOffMode;
            customConfig.MinDistance = _minDistance;
            customConfig.MaxDistance = _maxDistance;
            customConfig.Spread = _spread;
            customConfig.DopplerLevel = _dopplerLevel;
            customConfig.BypassEffects = _bypassEffects;
            customConfig.BypassListenerEffects = _bypassListenerEffects;
            customConfig.BypassReverbZones = _bypassReverbZones;
            customConfig.IgnoreListenerVolume = _ignoreListenerVolume;
            customConfig.IgnoreListenerPause = _ignoreListenerPause;

            return customConfig;
        }

        private AudioConfigurationSO CreateOverriddenConfiguration()
        {
            if (_audioConfiguration == null)
            {
                Debug.LogWarning("AudioConfiguration is null in Override mode. Using fully custom settings.", this);
                return CreateFullyCustomConfiguration();
            }

            var overriddenConfig = ScriptableObject.CreateInstance<AudioConfigurationSO>();

            // Start with base configuration values
            overriddenConfig.OutputAudioMixerGroup = _audioConfiguration.OutputAudioMixerGroup;
            overriddenConfig.Priority = _audioConfiguration.Priority;
            overriddenConfig.Mute = _audioConfiguration.Mute;
            overriddenConfig.Volume = _audioConfiguration.Volume;
            overriddenConfig.Pitch = _audioConfiguration.Pitch;
            overriddenConfig.PanStereo = _audioConfiguration.PanStereo;
            overriddenConfig.ReverbZoneMix = _audioConfiguration.ReverbZoneMix;
            overriddenConfig.SpatialBlend = _audioConfiguration.SpatialBlend;
            overriddenConfig.RollOffMode = _audioConfiguration.RollOffMode;
            overriddenConfig.MinDistance = _audioConfiguration.MinDistance;
            overriddenConfig.MaxDistance = _audioConfiguration.MaxDistance;
            overriddenConfig.Spread = _audioConfiguration.Spread;
            overriddenConfig.DopplerLevel = _audioConfiguration.DopplerLevel;
            overriddenConfig.BypassEffects = _audioConfiguration.BypassEffects;
            overriddenConfig.BypassListenerEffects = _audioConfiguration.BypassListenerEffects;
            overriddenConfig.BypassReverbZones = _audioConfiguration.BypassReverbZones;
            overriddenConfig.IgnoreListenerVolume = _audioConfiguration.IgnoreListenerVolume;
            overriddenConfig.IgnoreListenerPause = _audioConfiguration.IgnoreListenerPause;

            // Apply overrides where specified
            if (_overrideOutputMixerGroup) overriddenConfig.OutputAudioMixerGroup = _outputAudioMixerGroup;
            if (_overridePriority) overriddenConfig.Priority = (int)_priorityLevel;
            if (_overrideMute) overriddenConfig.Mute = _mute;
            if (_overrideVolume) overriddenConfig.Volume = _volume;
            if (_overridePitch) overriddenConfig.Pitch = _pitch;
            if (_overridePanStereo) overriddenConfig.PanStereo = _panStereo;
            if (_overrideReverbZoneMix) overriddenConfig.ReverbZoneMix = _reverbZoneMix;
            if (_overrideSpatialBlend) overriddenConfig.SpatialBlend = _spatialBlend;
            if (_overrideRollOffMode) overriddenConfig.RollOffMode = _rollOffMode;
            if (_overrideMinDistance) overriddenConfig.MinDistance = _minDistance;
            if (_overrideMaxDistance) overriddenConfig.MaxDistance = _maxDistance;
            if (_overrideSpread) overriddenConfig.Spread = _spread;
            if (_overrideDopplerLevel) overriddenConfig.DopplerLevel = _dopplerLevel;
            if (_overrideBypassEffects) overriddenConfig.BypassEffects = _bypassEffects;
            if (_overrideBypassListenerEffects) overriddenConfig.BypassListenerEffects = _bypassListenerEffects;
            if (_overrideBypassReverbZones) overriddenConfig.BypassReverbZones = _bypassReverbZones;
            if (_overrideIgnoreListenerVolume) overriddenConfig.IgnoreListenerVolume = _ignoreListenerVolume;
            if (_overrideIgnoreListenerPause) overriddenConfig.IgnoreListenerPause = _ignoreListenerPause;

            return overriddenConfig;
        }

        public void StopAudioCue()
        {
            if (_controlKey != AudioCueKey.Invalid)
            {
                if (!_audioCueEventChannel.RaiseStopEvent(_controlKey, _audioCue))
                {
                    _controlKey = AudioCueKey.Invalid;
                }
            }
        }

        public void FinishAudioCue()
        {
            if (_controlKey != AudioCueKey.Invalid)
            {
                if (!_audioCueEventChannel.RaiseFinishEvent(_controlKey))
                {
                    _controlKey = AudioCueKey.Invalid;
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            float minDist = 0.1f;
            float maxDist = 50f;

            if (_configurationMode == ConfigurationMode.UsePredefined && _audioConfiguration != null)
            {
                minDist = _audioConfiguration.MinDistance;
                maxDist = _audioConfiguration.MaxDistance;
            }
            else if (_configurationMode == ConfigurationMode.OverridePredefined)
            {
                if (_audioConfiguration != null)
                {
                    minDist = _overrideMinDistance ? _minDistance : _audioConfiguration.MinDistance;
                    maxDist = _overrideMaxDistance ? _maxDistance : _audioConfiguration.MaxDistance;
                }
                else
                {
                    minDist = _minDistance;
                    maxDist = _maxDistance;
                }
            }
            else if (_configurationMode == ConfigurationMode.FullyCustom)
            {
                minDist = _minDistance;
                maxDist = _maxDistance;
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
            if (_minDistance > _maxDistance)
            {
                _maxDistance = _minDistance;
            }
        }
    }
}