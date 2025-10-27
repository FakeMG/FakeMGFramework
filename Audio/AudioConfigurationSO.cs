using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;

namespace FakeMG.Framework.Audio
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.AUDIO + "/AudioConfigurationSO")]
    public class AudioConfigurationSO : ScriptableObject
    {
        public AudioMixerGroup OutputAudioMixerGroup;

        // Simplified management of priority levels (values are counterintuitive, see enum below)
        [SerializeField] private PriorityLevel _priorityLevel = PriorityLevel.Standard;

        public int Priority
        {
            get => (int)_priorityLevel;
            set => _priorityLevel = (PriorityLevel)value;
        }

        [Header("Sound properties")]
        public bool Mute;
        [Range(0f, 1f)] public float Volume = 1f;
        [Range(-3f, 3f)] public float Pitch = 1f;
        [Range(-1f, 1f)] public float PanStereo;
        [Range(0f, 1.1f)] public float ReverbZoneMix = 1f;

        [Header("Spatialization")]
        [Range(0f, 1f)] public float SpatialBlend = 1f;
        public AudioRolloffMode RollOffMode = AudioRolloffMode.Logarithmic;
        [MinValue(0f)]
        [OnValueChanged(nameof(ValidateDistances))]
        public float MinDistance = 0.1f;
        [MinValue(0.01f)]
        [OnValueChanged(nameof(ValidateDistances))]
        public float MaxDistance = 50f;
        [Range(0, 360)] public int Spread;
        [Range(0f, 5f)] public float DopplerLevel = 1f;

        [Header("Ignores")]
        public bool BypassEffects;
        public bool BypassListenerEffects;
        public bool BypassReverbZones;
        public bool IgnoreListenerVolume;
        public bool IgnoreListenerPause;

        public enum PriorityLevel
        {
            Highest = 0,
            High = 64,
            Standard = 128,
            Low = 194,
            VeryLow = 256,
        }

        private void ApplyTo(AudioSource audioSource)
        {
            audioSource.outputAudioMixerGroup = OutputAudioMixerGroup;
            audioSource.mute = Mute;
            audioSource.bypassEffects = BypassEffects;
            audioSource.bypassListenerEffects = BypassListenerEffects;
            audioSource.bypassReverbZones = BypassReverbZones;
            audioSource.priority = Priority;
            audioSource.volume = Volume;
            audioSource.pitch = Pitch;
            audioSource.panStereo = PanStereo;
            audioSource.spatialBlend = SpatialBlend;
            audioSource.reverbZoneMix = ReverbZoneMix;
            audioSource.dopplerLevel = DopplerLevel;
            audioSource.spread = Spread;
            audioSource.rolloffMode = RollOffMode;
            audioSource.minDistance = MinDistance;
            audioSource.maxDistance = MaxDistance;
            audioSource.ignoreListenerVolume = IgnoreListenerVolume;
            audioSource.ignoreListenerPause = IgnoreListenerPause;
        }

        public void ApplyToWithVariations(AudioSource audioSource, AudioCueSO audioCue)
        {
            // Apply base configuration first
            ApplyTo(audioSource);

            // Apply random variations if specified
            if (audioCue.RandomVolume > 0f)
            {
                float volumeMultiplier = Random.Range(1f - audioCue.RandomVolume, 1f + audioCue.RandomVolume);
                audioSource.volume = Mathf.Clamp01(audioSource.volume * volumeMultiplier);
            }

            if (audioCue.RandomPitch > 0f)
            {
                float pitchMultiplier = Random.Range(1f - audioCue.RandomPitch, 1f + audioCue.RandomPitch);
                audioSource.pitch = Mathf.Clamp(audioSource.pitch * pitchMultiplier, -3f, 3f);
            }
        }

        private void ValidateDistances()
        {
            if (MinDistance > MaxDistance)
            {
                MaxDistance = MinDistance;
            }
        }
    }
}