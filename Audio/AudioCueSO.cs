﻿using System;
using UnityEngine;

namespace FakeMG.FakeMGFramework.Audio
{
    [CreateAssetMenu(menuName = "Audio/AudioCueSO")]
    public class AudioCueSO : ScriptableObject
    {
        public bool looping;
        public bool fadeIn;
        public float fadeInDuration;
        public bool fadeOut;
        public float fadeOutDuration;
        [Tooltip("Time in seconds to wait before the audio cue can be played again")]
        [Range(0f, 10f)]
        public float replayDelay;
        [SerializeField] private AudioClipsGroup[] _audioClipGroups;

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