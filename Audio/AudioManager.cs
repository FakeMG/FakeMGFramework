using System.Collections.Generic;
using FakeMG.Framework.SOEventSystem.EventChannel;
using UnityEngine;
using UnityEngine.Audio;

namespace FakeMG.Framework.Audio
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
        [SerializeField] private SoundEmitter soundEmitterPrefab;
        [SerializeField] private SoundEmitter musicSoundEmitter;

        [Header("Listening on channels")]
        [Tooltip("The SoundManager listens to this event, fired by objects in any scene, to play SFXs")]
        [SerializeField] public AudioCueEventChannelSO sfxEventChannel;
        [SerializeField] public AudioCueEventChannelSO musicEventChannel;

        [Tooltip("The SoundManager listens to this event, fired by objects in any scene, to change SFXs volume")]
        [SerializeField] private FloatEventChannelSO sfxVolumeEventChannel;
        [Tooltip("The SoundManager listens to this event, fired by objects in any scene, to change Music volume")]
        [SerializeField] private FloatEventChannelSO musicVolumeEventChannel;
        [Tooltip("The SoundManager listens to this event, fired by objects in any scene, to change Master volume")]
        [SerializeField] private FloatEventChannelSO masterVolumeEventChannel;

        [Header("Audio control")]
        [SerializeField] private AudioMixer audioMixer;

        private Queue<SoundEmitter> _soundEmitterQueue;
        private SoundEmitterVault _soundEmitterVault;
        private readonly object _vaultLock = new();

        private void OnEnable()
        {
            sfxEventChannel.OnAudioCuePlayRequested += PlayAudioCue;
            sfxEventChannel.OnAudioCueStopRequested += StopAudioCue;
            sfxEventChannel.OnAudioCueFinishRequested += FinishAudioCue;

            musicEventChannel.OnAudioCuePlayRequested += PlayMusicTrack;
            musicEventChannel.OnAudioCueStopRequested += StopMusicTrack;

            sfxVolumeEventChannel.OnEventRaised += ChangeSfxVolume;
            musicVolumeEventChannel.OnEventRaised += ChangeMusicVolume;
            masterVolumeEventChannel.OnEventRaised += ChangeMasterVolume;
        }

        private void Start()
        {
            _soundEmitterQueue = new Queue<SoundEmitter>();
            _soundEmitterVault = new SoundEmitterVault();

            var sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_PREF, 1);
            var musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_PREF, 1);
            ChangeSfxVolume(sfxVolume);
            ChangeMusicVolume(musicVolume);
            ChangeMasterVolume(1);
        }

        private void OnDestroy()
        {
            CleanPool();

            // Stop any playing music
            if (musicSoundEmitter != null && musicSoundEmitter.IsPlaying())
            {
                musicSoundEmitter.Stop();
            }

            sfxEventChannel.OnAudioCuePlayRequested -= PlayAudioCue;
            sfxEventChannel.OnAudioCueStopRequested -= StopAudioCue;
            sfxEventChannel.OnAudioCueFinishRequested -= FinishAudioCue;

            musicEventChannel.OnAudioCuePlayRequested -= PlayMusicTrack;
            musicEventChannel.OnAudioCueStopRequested -= StopMusicTrack;

            sfxVolumeEventChannel.OnEventRaised -= ChangeSfxVolume;
            musicVolumeEventChannel.OnEventRaised -= ChangeMusicVolume;
            masterVolumeEventChannel.OnEventRaised -= ChangeMasterVolume;
        }

        private void ChangeMasterVolume(float newVolume)
        {
            float clampedVolume = Mathf.Max(newVolume, 0.0001f);
            SetGroupVolume(MASTER_VOLUME_PARAM, Mathf.Log10(clampedVolume) * 20);
            PlayerPrefs.SetFloat(MASTER_VOLUME_PREF, newVolume);
            PlayerPrefs.Save();
        }

        private void ChangeMusicVolume(float newVolume)
        {
            float clampedVolume = Mathf.Max(newVolume, 0.0001f);
            SetGroupVolume(MUSIC_VOLUME_PARAM, Mathf.Log10(clampedVolume) * 20);
            PlayerPrefs.SetFloat(MUSIC_VOLUME_PREF, newVolume);
            PlayerPrefs.Save();
        }

        private void ChangeSfxVolume(float newVolume)
        {
            float clampedVolume = Mathf.Max(newVolume, 0.0001f);
            SetGroupVolume(SFX_VOLUME_PARAM, Mathf.Log10(clampedVolume) * 20);
            PlayerPrefs.SetFloat(SFX_VOLUME_PREF, newVolume);
            PlayerPrefs.Save();
        }

        public void ToggleMusicVolume(bool isEnabled)
        {
            ChangeMusicVolume(isEnabled ? 1 : 0);
        }

        public void ToggleSfxVolume(bool isEnabled)
        {
            ChangeSfxVolume(isEnabled ? 1 : 0);
        }

        private void SetGroupVolume(string parameterName, float volume)
        {
            bool volumeSet = audioMixer.SetFloat(parameterName, volume);
            if (!volumeSet)
            {
                Debug.LogError("The AudioMixer parameter was not found");
            }
        }

        private AudioCueKey PlayMusicTrack(AudioCueSO audioCue, AudioConfigurationSO audioConfiguration,
            Vector3 positionInSpace, Transform parent = null)
        {
            float fadeDuration = 2f;

            if (musicSoundEmitter && musicSoundEmitter.IsPlaying())
            {
                AudioClip songToPlay = audioCue.GetClips()[0];

                //If the same song is already playing, do nothing
                if (musicSoundEmitter.GetClip() == songToPlay) return AudioCueKey.Invalid;

                //Music is already playing, need to fade it out
                musicSoundEmitter.FadeOutAudioClip(fadeDuration);
            }

            musicSoundEmitter.FadeInAudioClip(audioCue.GetClips()[0], audioConfiguration, audioCue);
            musicSoundEmitter.IgnoreListenerPause();

            return AudioCueKey.Invalid; //No need to return a valid key for music
        }

        private bool StopMusicTrack(AudioCueKey key, AudioCueSO audioCue)
        {
            if (musicSoundEmitter && musicSoundEmitter.IsPlaying())
            {
                musicSoundEmitter.Stop();
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

                if (soundEmitter)
                {
                    if (audioCueSO.fadeIn)
                    {
                        soundEmitter.FadeInAudioClip(audioClip, settings, audioCueSO);
                    }
                    else
                    {
                        soundEmitter.Play(audioClip, settings, audioCueSO, position);
                    }

                    soundEmitter.OnSoundFinishedPlaying += CleanEmitter;
                }
            }

            return key;
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

            foreach (var emitter in soundEmitters)
            {
                if (audioCue.fadeOut)
                {
                    emitter.FadeOutAudioClip(audioCue.fadeOutDuration);
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
            soundEmitter.OnSoundFinishedPlaying -= CleanEmitter;

            ReleaseSoundEmitter(soundEmitter);

            // Clean emitter from vault with thread safety
            lock (_vaultLock)
            {
                bool isFound =
                    _soundEmitterVault.Get(soundEmitter.AudioCueKey, out List<SoundEmitter> soundEmitterList);
                if (isFound)
                {
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
            }
        }

        private SoundEmitter GetSoundEmitter()
        {
            if (_soundEmitterQueue.TryDequeue(out SoundEmitter soundEmitter))
            {
                soundEmitter.gameObject.SetActive(true);
                return soundEmitter;
            }

            soundEmitter = Instantiate(soundEmitterPrefab, transform, true);
            soundEmitter.gameObject.SetActive(true);
            return soundEmitter;
        }

        private void ReleaseSoundEmitter(SoundEmitter soundEmitter)
        {
            soundEmitter.gameObject.SetActive(false);
            _soundEmitterQueue.Enqueue(soundEmitter);
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