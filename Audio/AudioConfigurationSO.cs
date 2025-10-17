using UnityEngine;
using UnityEngine.Audio;

namespace FakeMG.Framework.Audio
{
    [CreateAssetMenu(menuName = "Audio/AudioConfigurationSO")]
    public class AudioConfigurationSO : ScriptableObject
    {
        public AudioMixerGroup outputAudioMixerGroup;

        // Simplified management of priority levels (values are counterintuitive, see enum below)
        [SerializeField] private PriorityLevel priorityLevel = PriorityLevel.Standard;

        public int Priority
        {
            get => (int)priorityLevel;
            set => priorityLevel = (PriorityLevel)value;
        }

        [Header("Sound properties")]
        public bool mute;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(-3f, 3f)] public float pitch = 1f;
        [Range(-1f, 1f)] public float panStereo;
        [Range(0f, 1.1f)] public float reverbZoneMix = 1f;

        [Header("Spatialization")]
        [Range(0f, 1f)] public float spatialBlend = 1f;
        public AudioRolloffMode rollOffMode = AudioRolloffMode.Logarithmic;
        [Range(0.01f, 5f)] public float minDistance = 0.1f;
        [Range(5f, 100f)] public float maxDistance = 50f;
        [Range(0, 360)] public int spread;
        [Range(0f, 5f)] public float dopplerLevel = 1f;

        [Header("Ignores")]
        public bool bypassEffects;
        public bool bypassListenerEffects;
        public bool bypassReverbZones;
        public bool ignoreListenerVolume;
        public bool ignoreListenerPause;

        private enum PriorityLevel
        {
            Highest = 0,
            High = 64,
            Standard = 128,
            Low = 194,
            VeryLow = 256,
        }

        private void ApplyTo(AudioSource audioSource)
        {
            audioSource.outputAudioMixerGroup = outputAudioMixerGroup;
            audioSource.mute = mute;
            audioSource.bypassEffects = bypassEffects;
            audioSource.bypassListenerEffects = bypassListenerEffects;
            audioSource.bypassReverbZones = bypassReverbZones;
            audioSource.priority = Priority;
            audioSource.volume = volume;
            audioSource.pitch = pitch;
            audioSource.panStereo = panStereo;
            audioSource.spatialBlend = spatialBlend;
            audioSource.reverbZoneMix = reverbZoneMix;
            audioSource.dopplerLevel = dopplerLevel;
            audioSource.spread = spread;
            audioSource.rolloffMode = rollOffMode;
            audioSource.minDistance = minDistance;
            audioSource.maxDistance = maxDistance;
            audioSource.ignoreListenerVolume = ignoreListenerVolume;
            audioSource.ignoreListenerPause = ignoreListenerPause;
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
    }
}