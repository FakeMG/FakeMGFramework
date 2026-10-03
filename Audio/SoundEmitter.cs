using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.Audio;

namespace FakeMG.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundEmitter : MonoBehaviour
    {
        private AudioSource _audioSource;
        private CancellationTokenSource _completionCancellationSource;

        public event Action<SoundEmitter> OnSoundFinishedPlaying;
        public event Action<SoundEmitter> OnSoundDestroyed;
        public AudioCueKey AudioCueKey;

        #region Unity Lifecycle

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.ignoreListenerPause = false;
        }

        private void OnDestroy()
        {
            CancelCompletionWait();
            _audioSource.DOKill();
            OnSoundDestroyed?.Invoke(this);
        }

        #endregion

        #region Public Methods

        public void Play(
            AudioClip clip,
            AudioConfigurationSO audioConfigSO,
            AudioCueSO audioCueSO,
            AudioMixerGroup outputAudioMixerGroup,
            Vector3 position = default)
        {
            CancelCompletionWait();
            _audioSource.DOKill();
            _audioSource.clip = clip;
            _audioSource.outputAudioMixerGroup = outputAudioMixerGroup;
            audioConfigSO.ApplyToWithVariations(_audioSource, audioCueSO);
            _audioSource.transform.position = position;
            _audioSource.loop = audioCueSO.Looping;
            _audioSource.time = audioCueSO.RandomStartTime ? UnityEngine.Random.Range(0f, clip.length) : 0f;
            _audioSource.Play();
            if (!audioCueSO.Looping)
            {
                StartCompletionWait();
            }
        }

        public void Stop()
        {
            CancelCompletionWait();
            _audioSource.DOKill();
            _audioSource.Stop();
            NotifySoundFinishedPlaying();
        }

        public void Finish()
        {
            if (_audioSource.loop)
            {
                _audioSource.loop = false;
                StartCompletionWait();
            }
        }

        public void FadeInAudioClip(
            AudioClip musicClip,
            AudioConfigurationSO audioConfigSO,
            AudioCueSO audioCueSO,
            AudioMixerGroup outputAudioMixerGroup)
        {
            Play(musicClip, audioConfigSO, audioCueSO, outputAudioMixerGroup);
            float targetVolume = _audioSource.volume;
            _audioSource.volume = 0f;
            _audioSource.DOFade(targetVolume, audioCueSO.FadeInDuration).SetUpdate(true);
        }

        public void FadeOutAudioClip(float durationSeconds)
        {
            CancelCompletionWait();
            _audioSource.DOKill();
            _audioSource.DOFade(0f, durationSeconds).SetUpdate(true).SetLink(gameObject).OnComplete(Stop);
        }

        public AudioClip GetClip() => _audioSource.clip;

        public bool IsPlaying() => _audioSource.isPlaying;

        public void IgnoreListenerPause() => _audioSource.ignoreListenerPause = true;

        #endregion

        #region Private Methods

        private void StartCompletionWait()
        {
            CancelCompletionWait();
            _completionCancellationSource = new CancellationTokenSource();
            WaitForPlaybackCompletionAsync(_completionCancellationSource.Token).Forget();
        }

        private async UniTask WaitForPlaybackCompletionAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Audio playback uses the DSP clock, independent of simulation speed and clip pitch.
                await UniTask.WaitUntil(HasPlaybackFinished, cancellationToken: cancellationToken);
                NotifySoundFinishedPlaying();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Echo.Log("Sound completion wait cancelled because playback stopped or the emitter was destroyed.");
            }
            catch (Exception exception)
            {
                Echo.Error(exception.ToString());
            }
        }

        private bool HasPlaybackFinished() => !_audioSource.isPlaying;

        private void CancelCompletionWait()
        {
            if (_completionCancellationSource != null)
            {
                _completionCancellationSource.Cancel();
                _completionCancellationSource.Dispose();
                _completionCancellationSource = null;
            }
        }

        private void NotifySoundFinishedPlaying()
        {
            if (OnSoundFinishedPlaying != null)
            {
                OnSoundFinishedPlaying.Invoke(this);
            }
            else
            {
                Echo.Warning("Sound emitter finished without a completion subscriber.", context: this);
            }
        }

        #endregion
    }
}
