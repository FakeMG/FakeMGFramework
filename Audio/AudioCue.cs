using System.Collections;
using UnityEngine;

namespace FakeMG.FakeMGFramework.Audio
{
    public class AudioCue : MonoBehaviour
    {
        [Header("Sound definition")]
        [SerializeField] private AudioCueSO audioCue;
        [SerializeField] private bool playOnStart;
        [SerializeField] private bool stopOnDisable;
        [SerializeField] private float startDelay;

        [Header("Configuration")]
        [SerializeField] private AudioCueEventChannelSO audioCueEventChannel;
        [SerializeField] private AudioConfigurationSO audioConfiguration;
        [SerializeField] private bool followParent;

        private AudioCueKey _controlKey = AudioCueKey.Invalid;
        private float _lastPlayTime = -1f;

        private void Start()
        {
            if (playOnStart)
                StartCoroutine(PlayDelayed());
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
            if (CanPlaySound())
            {
                var transformToUse = followParent ? transform : null;
                _controlKey =
                    audioCueEventChannel.RaisePlayEvent(audioCue, audioConfiguration, transform.position, transformToUse);
                _lastPlayTime = Time.time;
            }
        }

        private bool CanPlaySound()
        {
            const float defaultLastPlayTime = -1f;
            const float noDelay = 0f;

            return audioCue.replayDelay <= noDelay || _lastPlayTime == defaultLastPlayTime ||
                   Time.time >= _lastPlayTime + audioCue.replayDelay;
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
    }
}