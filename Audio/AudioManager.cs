using System.Collections.Generic;
using FakeMG.Framework.EventBus;
using UnityEngine;
using UnityEngine.Audio;

namespace FakeMG.Audio
{
    public class AudioManager : MonoBehaviour
    {
        // Audio mixer parameter names
        private const string MASTER_VOLUME_PARAM = "MasterVolume";
        private const string MUSIC_VOLUME_PARAM = "MusicVolume";
        private const string SFX_VOLUME_PARAM = "SFXVolume";

        // PlayerPrefs keys
        private const string MASTER_VOLUME_PREF = "MasterVolume";
        private const string MUSIC_VOLUME_PREF = "MusicVolume";
        private const string SFX_VOLUME_PREF = "SFXVolume";

        [Header("Prefab")]
        [SerializeField] private SoundEmitter _soundEmitterPrefab;
        [SerializeField] private SoundEmitter _musicSoundEmitter;

        [Header("Listening on channels")]
        [Tooltip("The SoundManager listens to this event, fired by objects in any scene, to play SFXs")]
        [SerializeField] private AudioCueEventChannelSO _sfxEventChannel;
        [SerializeField] private AudioCueEventChannelSO _musicEventChannel;

        [Header("Audio control")]
        [SerializeField] private AudioMixer _audioMixer;

        private Queue<SoundEmitter> _soundEmitterQueue;
        private SoundEmitterVault _soundEmitterVault;
        private readonly object _vaultLock = new();

        private void OnEnable()
        {
            _sfxEventChannel.OnAudioCuePlayRequested += PlayAudioCue;
            _sfxEventChannel.OnAudioCueStopRequested += StopAudioCue;
            _sfxEventChannel.OnAudioCueFinishRequested += FinishAudioCue;

            _musicEventChannel.OnAudioCuePlayRequested += PlayMusicTrack;
            _musicEventChannel.OnAudioCueStopRequested += StopMusicTrack;

            EventBus<SfxVolumeChangedEvent>.OnEvent += ApplySfxVolumeWhenChanged;
            EventBus<MusicVolumeChangedEvent>.OnEvent += ApplyMusicVolumeWhenChanged;
            EventBus<MasterVolumeChangedEvent>.OnEvent += ApplyMasterVolumeWhenChanged;
        }

        private void Start()
        {
            _soundEmitterQueue = new Queue<SoundEmitter>();
            _soundEmitterVault = new SoundEmitterVault();

            var sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_PREF, 1);
            var musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_PREF, 1);
            ApplyVolumeChange(SFX_VOLUME_PARAM, SFX_VOLUME_PREF, sfxVolume);
            ApplyVolumeChange(MUSIC_VOLUME_PARAM, MUSIC_VOLUME_PREF, musicVolume);
            ApplyVolumeChange(MASTER_VOLUME_PARAM, MASTER_VOLUME_PREF, 1);
        }

        private void OnDestroy()
        {
            CleanPool();

            // Stop any playing music
            if (_musicSoundEmitter != null && _musicSoundEmitter.IsPlaying())
            {
                _musicSoundEmitter.Stop();
            }

            _sfxEventChannel.OnAudioCuePlayRequested -= PlayAudioCue;
            _sfxEventChannel.OnAudioCueStopRequested -= StopAudioCue;
            _sfxEventChannel.OnAudioCueFinishRequested -= FinishAudioCue;

            _musicEventChannel.OnAudioCuePlayRequested -= PlayMusicTrack;
            _musicEventChannel.OnAudioCueStopRequested -= StopMusicTrack;

            EventBus<SfxVolumeChangedEvent>.OnEvent -= ApplySfxVolumeWhenChanged;
            EventBus<MusicVolumeChangedEvent>.OnEvent -= ApplyMusicVolumeWhenChanged;
            EventBus<MasterVolumeChangedEvent>.OnEvent -= ApplyMasterVolumeWhenChanged;
        }

        private void ApplySfxVolumeWhenChanged(SfxVolumeChangedEvent volumeChangedEvent)
        {
            float newVolume = volumeChangedEvent.Volume;
            ApplyVolumeChange(SFX_VOLUME_PARAM, SFX_VOLUME_PREF, newVolume);
        }

        private void ApplyMusicVolumeWhenChanged(MusicVolumeChangedEvent volumeChangedEvent)
        {
            float newVolume = volumeChangedEvent.Volume;
            ApplyVolumeChange(MUSIC_VOLUME_PARAM, MUSIC_VOLUME_PREF, newVolume);
        }

        private void ApplyMasterVolumeWhenChanged(MasterVolumeChangedEvent volumeChangedEvent)
        {
            float newVolume = volumeChangedEvent.Volume;
            ApplyVolumeChange(MASTER_VOLUME_PARAM, MASTER_VOLUME_PREF, newVolume);
        }

        private void ApplyVolumeChange(string mixerParameterName, string playerPrefsKey, float newVolume)
        {
            const float MIN_LINEAR_VOLUME = 0.0001f;
            const float DECIBEL_SCALE = 20f;

            // Keeps conversion numerically stable for silent volume.
            float clampedVolume = Mathf.Max(newVolume, MIN_LINEAR_VOLUME);
            SetGroupVolume(mixerParameterName, Mathf.Log10(clampedVolume) * DECIBEL_SCALE);
            PlayerPrefs.SetFloat(playerPrefsKey, newVolume);
            PlayerPrefs.Save();
        }

        public void ToggleMusicVolume(bool isEnabled)
        {
            float volume = isEnabled ? 1 : 0;
            ApplyVolumeChange(MUSIC_VOLUME_PARAM, MUSIC_VOLUME_PREF, volume);
        }

        public void ToggleSfxVolume(bool isEnabled)
        {
            float volume = isEnabled ? 1 : 0;
            ApplyVolumeChange(SFX_VOLUME_PARAM, SFX_VOLUME_PREF, volume);
        }

        private void SetGroupVolume(string parameterName, float volume)
        {
            bool volumeSet = _audioMixer.SetFloat(parameterName, volume);
            if (!volumeSet)
            {
                Debug.LogError("The AudioMixer parameter was not found");
            }
        }

        private AudioCueKey PlayMusicTrack(AudioCueSO audioCue, AudioConfigurationSO audioConfiguration,
            Vector3 positionInSpace, Transform parent = null)
        {
            float fadeDuration = 2f;

            if (_musicSoundEmitter && _musicSoundEmitter.IsPlaying())
            {
                AudioClip songToPlay = audioCue.GetClips()[0];

                //If the same song is already playing, do nothing
                if (_musicSoundEmitter.GetClip() == songToPlay) return AudioCueKey.Invalid;

                //Music is already playing, need to fade it out
                _musicSoundEmitter.FadeOutAudioClip(fadeDuration);
            }

            _musicSoundEmitter.FadeInAudioClip(audioCue.GetClips()[0], audioConfiguration, audioCue);
            _musicSoundEmitter.IgnoreListenerPause();

            return AudioCueKey.Invalid; //No need to return a valid key for music
        }

        private bool StopMusicTrack(AudioCueKey key, AudioCueSO audioCue)
        {
            if (_musicSoundEmitter && _musicSoundEmitter.IsPlaying())
            {
                _musicSoundEmitter.Stop();
                return true;
            }

            return false;
        }

        private AudioCueKey PlayAudioCue(AudioCueSO audioCueSO, AudioConfigurationSO settings,
            Vector3 position = default, Transform parent = null)
        {
            AudioClip[] clipsToPlay = audioCueSO.GetClips();
            List<SoundEmitter> soundEmitterList = new();

            AudioCueKey key;
            lock (_vaultLock)
            {
                key = _soundEmitterVault.Add(audioCueSO, soundEmitterList);
            }

            foreach (var audioClip in clipsToPlay)
            {
                InitializeSoundEmitter(audioCueSO, settings, key, soundEmitterList, audioClip, parent, position);
            }

            return key;
        }

        private void InitializeSoundEmitter(AudioCueSO audioCueSO, AudioConfigurationSO settings, AudioCueKey key,
            List<SoundEmitter> soundEmitterList, AudioClip audioClip, Transform parent, Vector3 position)
        {
            var soundEmitter = GetSoundEmitter();
            soundEmitter.AudioCueKey = key;

            lock (_vaultLock)
            {
                soundEmitterList.Add(soundEmitter);
            }

            if (parent)
            {
                soundEmitter.transform.SetParent(parent);
            }

            if (audioCueSO.FadeIn)
            {
                soundEmitter.FadeInAudioClip(audioClip, settings, audioCueSO);
            }
            else
            {
                soundEmitter.Play(audioClip, settings, audioCueSO, position);
            }

            soundEmitter.OnSoundFinishedPlaying += CleanEmitter;
            soundEmitter.OnSoundDestroyed += RemoveEmitter;
        }

        private bool StopAudioCue(AudioCueKey audioCueKey, AudioCueSO audioCue)
        {
            List<SoundEmitter> soundEmitters;
            lock (_vaultLock)
            {
                bool isFound = _soundEmitterVault.Get(audioCueKey, out soundEmitters);
                if (!isFound)
                    return false;
            }

            // Iterate backwards to safely handle modifications during iteration
            for (int i = soundEmitters.Count - 1; i >= 0; i--)
            {
                var emitter = soundEmitters[i];
                if (audioCue.FadeOut)
                {
                    emitter.FadeOutAudioClip(audioCue.FadeOutDuration);
                }
                else
                {
                    emitter.Stop();
                }
            }

            return true;
        }

        private bool FinishAudioCue(AudioCueKey audioCueKey)
        {
            List<SoundEmitter> soundEmitterList;
            lock (_vaultLock)
            {
                bool isFound = _soundEmitterVault.Get(audioCueKey, out soundEmitterList);
                if (!isFound)
                {
                    Debug.LogWarning("Finishing an AudioCue was requested, but the AudioCue was not found.");
                    return false;
                }
            }

            foreach (var emitter in soundEmitterList)
            {
                emitter.Finish();
            }

            return true;
        }

        private void CleanEmitter(SoundEmitter soundEmitter)
        {
            lock (_vaultLock)
            {
                TryRemoveEmitterFromVault(soundEmitter);
            }

            soundEmitter.OnSoundFinishedPlaying -= CleanEmitter;
            soundEmitter.OnSoundDestroyed -= RemoveEmitter;
            soundEmitter.transform.SetParent(transform);

            ReleaseSoundEmitter(soundEmitter);
        }

        private void RemoveEmitter(SoundEmitter soundEmitter)
        {
            lock (_vaultLock)
            {
                TryRemoveEmitterFromVault(soundEmitter);
            }
            RemoveEmitterFromPool(soundEmitter);
        }

        private void TryRemoveEmitterFromVault(SoundEmitter soundEmitter)
        {
            if (!_soundEmitterVault.Get(soundEmitter.AudioCueKey, out List<SoundEmitter> soundEmitterList))
                return;

            // First, remove the current emitter from the list
            soundEmitterList.Remove(soundEmitter);

            // Then check if the list is empty or all remaining emitters have stopped
            bool shouldRemoveKey = soundEmitterList.Count == 0;
            if (!shouldRemoveKey)
            {
                bool allEmittersStopped = true;
                foreach (var soundEmitterInList in soundEmitterList)
                {
                    if (soundEmitterInList.IsPlaying())
                    {
                        allEmittersStopped = false;
                        break;
                    }
                }

                shouldRemoveKey = allEmittersStopped;
            }

            if (shouldRemoveKey)
            {
                _soundEmitterVault.Remove(soundEmitter.AudioCueKey);
            }
        }

        private SoundEmitter GetSoundEmitter()
        {
            if (_soundEmitterQueue.TryDequeue(out SoundEmitter soundEmitter))
            {
                soundEmitter.gameObject.SetActive(true);
                return soundEmitter;
            }

            soundEmitter = Instantiate(_soundEmitterPrefab, transform, true);
            soundEmitter.gameObject.SetActive(true);
            return soundEmitter;
        }

        private void ReleaseSoundEmitter(SoundEmitter soundEmitter)
        {
            soundEmitter.gameObject.SetActive(false);
            _soundEmitterQueue.Enqueue(soundEmitter);
        }

        private void RemoveEmitterFromPool(SoundEmitter soundEmitter)
        {
            int count = _soundEmitterQueue.Count;
            for (int i = 0; i < count; i++)
            {
                var emitter = _soundEmitterQueue.Dequeue();
                if (emitter != soundEmitter)
                {
                    _soundEmitterQueue.Enqueue(emitter);
                }
            }
        }

        public void CleanPool()
        {
            foreach (var soundEmitter in _soundEmitterQueue)
            {
                Destroy(soundEmitter.gameObject);
            }

            lock (_vaultLock)
            {
                _soundEmitterVault = new SoundEmitterVault();
            }
        }
    }
}