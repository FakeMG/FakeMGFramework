using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.FakeMGFramework.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        private AudioSource _audioSource;
        public event UnityAction<SoundEmitter> OnSoundFinishedPlaying;
        public AudioCueKey AudioCueKey;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.ignoreListenerPause = false;
        }

        public void Play(
            AudioClip clip, AudioConfigurationSO settings, bool hasToLoop,
            Vector3 position = default)
        {
            _audioSource.clip = clip;
            settings.ApplyTo(_audioSource);
            _audioSource.transform.position = position;
            _audioSource.loop = hasToLoop;
            _audioSource.time = hasToLoop ? Random.Range(0f, clip.length) : 0f;
            _audioSource.Play();

            if (!hasToLoop)
            {
                StartCoroutine(FinishedPlaying(clip.length));
            }
        }

        public void Stop()
        {
            _audioSource.Stop();
            StopAllCoroutines();
            NotifyBeingDone();
        }

        public void Finish()
        {
            if (_audioSource.loop)
            {
                _audioSource.loop = false;
                float timeRemaining = _audioSource.clip.length - _audioSource.time;
                StartCoroutine(FinishedPlaying(timeRemaining));
            }
        }

        private IEnumerator FinishedPlaying(float clipLength)
        {
            yield return new WaitForSeconds(clipLength);

            NotifyBeingDone();
        }

        private void NotifyBeingDone()
        {
            if (OnSoundFinishedPlaying != null)
            {
                OnSoundFinishedPlaying.Invoke(this);
            }
            else
            {
                // Sometimes sound emitters are stopped twice
                Debug.LogWarning("No listeners for OnSoundFinishedPlaying event.", this);
            }
        }

        public void FadeInAudioClip(AudioClip musicClip, AudioConfigurationSO settings, float duration)
        {
            Play(musicClip, settings, true);
            _audioSource.volume = 0f;

            _audioSource.DOFade(settings.volume, duration);
        }

        public void FadeOutAudioClip(float duration)
        {
            _audioSource.DOFade(0f, duration).onComplete += NotifyBeingDone;
        }

        public AudioClip GetClip()
        {
            return _audioSource.clip;
        }

        public bool IsPlaying()
        {
            return _audioSource.isPlaying;
        }

        public void IgnoreListenerPause()
        {
            _audioSource.ignoreListenerPause = true;
        }
    }
}