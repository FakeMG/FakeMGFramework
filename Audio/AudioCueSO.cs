using System;
using UnityEngine;

namespace FakeMG.Framework.Audio
{
    [CreateAssetMenu(menuName = "Audio/AudioCueSO")]
    public class AudioCueSO : ScriptableObject
    {
        [SerializeField] private bool looping;
        [SerializeField] private bool fadeIn;
        [SerializeField] private float fadeInDuration;
        [SerializeField] private bool fadeOut;
        [SerializeField] private float fadeOutDuration;
        [Tooltip("Time in seconds to wait before the audio cue can be played again")]
        [Range(0f, 10f)]
        [SerializeField] private float replayDelay;

        [Header("Randomization")]
        [Tooltip("Random volume variation range. Final volume = base volume * Random.Range(1 - randomVolume, 1 + randomVolume)")]
        [Range(0f, 1f)]
        [SerializeField] private float randomVolume;
        [Tooltip("Random pitch variation range. Final pitch = base pitch * Random.Range(1 - randomPitch, 1 + randomPitch)")]
        [Range(0f, 1f)]
        [SerializeField] private float randomPitch;
        [Tooltip("When enabled, starts audio playback at a random time within the clip duration")]
        [SerializeField] private bool randomStartTime;

        [SerializeField] private AudioClipsGroup[] _audioClipGroups;

        private float _lastPlayTime = -1f;

        public bool Looping => looping;
        public bool FadeIn => fadeIn;
        public float FadeInDuration => fadeInDuration;
        public bool FadeOut => fadeOut;
        public float FadeOutDuration => fadeOutDuration;
        public float ReplayDelay => replayDelay;
        public float RandomVolume => randomVolume;
        public float RandomPitch => randomPitch;
        public bool RandomStartTime => randomStartTime;

        public AudioClip[] GetClips()
        {
            int numberOfClips = _audioClipGroups.Length;
            AudioClip[] resultingClips = new AudioClip[numberOfClips];

            for (int i = 0; i < numberOfClips; i++)
            {
                resultingClips[i] = _audioClipGroups[i].GetNextClip();
            }

            return resultingClips;
        }

        public bool CanPlaySound()
        {
            const float defaultLastPlayTime = -1f;
            const float noDelay = 0f;

            return replayDelay <= noDelay || _lastPlayTime == defaultLastPlayTime ||
                   Time.time >= _lastPlayTime + replayDelay;
        }

        public void UpdateLastPlayTime()
        {
            _lastPlayTime = Time.time;
        }
    }

    /// <summary>
    /// Represents a group of AudioClips that can be treated as one, and provides automatic randomisation or sequencing based on the <c>SequenceMode</c> value.
    /// </summary>
    [Serializable]
    public class AudioClipsGroup
    {
        public SequenceMode sequenceMode = SequenceMode.RandomNoImmediateRepeat;
        public AudioClip[] audioClips;

        private int _nextClipToPlay = -1;
        private int _lastClipPlayed = -1;

        /// <summary>
        /// Chooses the next clip in the sequence, either following the order or randomly.
        /// </summary>
        /// <returns>A reference to an AudioClip</returns>
        public AudioClip GetNextClip()
        {
            // Fast out if there is only one clip to play
            if (audioClips.Length == 1)
                return audioClips[0];

            if (_nextClipToPlay == -1)
            {
                // Index needs to be initialised: 0 if Sequential, random if otherwise
                _nextClipToPlay = (sequenceMode == SequenceMode.Sequential)
                    ? 0
                    : UnityEngine.Random.Range(0, audioClips.Length);
            }
            else
            {
                // Select next clip index based on the appropriate SequenceMode
                switch (sequenceMode)
                {
                    case SequenceMode.Random:
                        _nextClipToPlay = UnityEngine.Random.Range(0, audioClips.Length);
                        break;

                    case SequenceMode.RandomNoImmediateRepeat:
                        do
                        {
                            _nextClipToPlay = UnityEngine.Random.Range(0, audioClips.Length);
                        }
                        while (_nextClipToPlay == _lastClipPlayed && audioClips.Length > 1);

                        break;

                    case SequenceMode.Sequential:
                        _nextClipToPlay = (int)Mathf.Repeat(++_nextClipToPlay, audioClips.Length);
                        break;
                }
            }

            _lastClipPlayed = _nextClipToPlay;

            return audioClips[_nextClipToPlay];
        }

        public enum SequenceMode
        {
            Random,
            RandomNoImmediateRepeat,
            Sequential,
        }
    }
}