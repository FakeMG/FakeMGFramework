using System;
using UnityEngine;

namespace FakeMG.Framework.Audio
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.AUDIO + "/AudioCueSO")]
    public class AudioCueSO : ScriptableObject
    {
        [SerializeField] private bool _looping;
        [SerializeField] private bool _fadeIn;
        [SerializeField] private float _fadeInDuration;
        [SerializeField] private bool _fadeOut;
        [SerializeField] private float _fadeOutDuration;
        [Tooltip("Time in seconds to wait before the audio cue can be played again")]
        [Range(0f, 10f)]
        [SerializeField] private float _replayDelay;

        [Header("Randomization")]
        [Tooltip("Random volume variation range. Final volume = base volume * Random.Range(1 - randomVolume, 1 + randomVolume)")]
        [Range(0f, 1f)]
        [SerializeField] private float _randomVolume;
        [Tooltip("Random pitch variation range. Final pitch = base pitch * Random.Range(1 - randomPitch, 1 + randomPitch)")]
        [Range(0f, 1f)]
        [SerializeField] private float _randomPitch;
        [Tooltip("When enabled, starts audio playback at a random time within the clip duration")]
        [SerializeField] private bool _randomStartTime;

        [SerializeField] private AudioClipsGroup[] _audioClipGroups;

        private float _lastPlayTime = -1f;

        public bool Looping => _looping;
        public bool FadeIn => _fadeIn;
        public float FadeInDuration => _fadeInDuration;
        public bool FadeOut => _fadeOut;
        public float FadeOutDuration => _fadeOutDuration;
        public float ReplayDelay => _replayDelay;
        public float RandomVolume => _randomVolume;
        public float RandomPitch => _randomPitch;
        public bool RandomStartTime => _randomStartTime;

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
            const float DEFAULT_LAST_PLAY_TIME = -1f;
            const float NO_DELAY = 0f;

            return _replayDelay <= NO_DELAY || _lastPlayTime == DEFAULT_LAST_PLAY_TIME ||
                   Time.time >= _lastPlayTime + _replayDelay;
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
        [SerializeField] private SequenceMode _sequenceMode = SequenceMode.RandomNoImmediateRepeat;
        public AudioClip[] AudioClips;

        private int _nextClipToPlay = -1;
        private int _lastClipPlayed = -1;

        /// <summary>
        /// Chooses the next clip in the sequence, either following the order or randomly.
        /// </summary>
        /// <returns>A reference to an AudioClip</returns>
        public AudioClip GetNextClip()
        {
            // Fast out if there is only one clip to play
            if (AudioClips.Length == 1)
                return AudioClips[0];

            if (_nextClipToPlay == -1)
            {
                // Index needs to be initialised: 0 if Sequential, random if otherwise
                _nextClipToPlay = (_sequenceMode == SequenceMode.Sequential)
                    ? 0
                    : UnityEngine.Random.Range(0, AudioClips.Length);
            }
            else
            {
                // Select next clip index based on the appropriate SequenceMode
                switch (_sequenceMode)
                {
                    case SequenceMode.Random:
                        _nextClipToPlay = UnityEngine.Random.Range(0, AudioClips.Length);
                        break;

                    case SequenceMode.RandomNoImmediateRepeat:
                        do
                        {
                            _nextClipToPlay = UnityEngine.Random.Range(0, AudioClips.Length);
                        }
                        while (_nextClipToPlay == _lastClipPlayed && AudioClips.Length > 1);

                        break;

                    case SequenceMode.Sequential:
                        _nextClipToPlay = (int)Mathf.Repeat(++_nextClipToPlay, AudioClips.Length);
                        break;
                }
            }

            _lastClipPlayed = _nextClipToPlay;

            return AudioClips[_nextClipToPlay];
        }

        public enum SequenceMode
        {
            Random,
            RandomNoImmediateRepeat,
            Sequential,
        }
    }
}