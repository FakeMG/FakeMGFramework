using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Settings;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;

namespace FakeMG.Audio
{
    public class AudioManager : MonoBehaviour
    {
        private const float MUSIC_FADE_DURATION_SECONDS = 2f;

        [Header("Prefab")]
        [SerializeField] private SoundEmitter _soundEmitterPrefab;
        [SerializeField] private SoundEmitter _musicSoundEmitter;

        [Header("Listening on channels")]
        [Tooltip("Any channel in this list will be handled by the pooled playback pipeline.")]
        [SerializeField] private List<AudioCueEventChannelSO> _pooledEventChannels = new();
        [Tooltip("Music uses a dedicated emitter to preserve crossfade behavior.")]
        [SerializeField] private AudioCueEventChannelSO _musicEventChannel;

        [Inject] private readonly SettingsStateRepository _settingsStateRepository;

        private Queue<SoundEmitter> _soundEmitterQueue;
        private SoundEmitterVault _soundEmitterVault;
        private AudioChannelRegistry _channelRegistry;
        private readonly object _vaultLock = new();
        private readonly HashSet<SoundEmitter> _ownedSoundEmitters = new();

        #region Unity Lifecycle

        private void Awake()
        {
            _soundEmitterQueue = new Queue<SoundEmitter>();
            _soundEmitterVault = new SoundEmitterVault();
            _channelRegistry = new AudioChannelRegistry(_pooledEventChannels, _musicEventChannel);
        }

        private void OnEnable()
        {
            _channelRegistry.RegisterPooledChannels(PlayAudioCue, StopAudioCue, FinishAudioCue, UpdateAudioCuePlayback);
            _channelRegistry.RegisterMusicChannel(PlayMusicTrack, StopMusicTrack);
        }

        private void Start()
        {
            _channelRegistry.SubscribeToVolumeChanges(_settingsStateRepository);
            _channelRegistry.InitializeChannelVolumes(_settingsStateRepository);
        }

        private void OnDisable()
        {
            _channelRegistry.UnsubscribeFromVolumeChanges(_settingsStateRepository);
            _channelRegistry.UnregisterPooledChannels();
            _channelRegistry.UnregisterMusicChannel();

            CleanPool();

            if (_musicSoundEmitter && _musicSoundEmitter.IsPlaying())
            {
                _musicSoundEmitter.Stop();
            }
        }

        private void OnValidate()
        {
            AudioChannelRegistry.ValidatePooledChannelConfiguration(_pooledEventChannels, _musicEventChannel);
        }

        #endregion

        #region Public Methods

        public void CleanPool()
        {
            foreach (SoundEmitter soundEmitter in _ownedSoundEmitters)
            {
                soundEmitter.OnSoundFinishedPlaying -= CleanEmitter;
                soundEmitter.OnSoundDestroyed -= RemoveEmitter;
                soundEmitter.gameObject.SetActive(false);
                Destroy(soundEmitter.gameObject);
            }

            _ownedSoundEmitters.Clear();
            _soundEmitterQueue.Clear();
            lock (_vaultLock)
            {
                _soundEmitterVault = new SoundEmitterVault();
            }
        }

        #endregion

        #region Private Methods

        private AudioCueKey PlayMusicTrack(
            AudioCueSO audioCue,
            AudioConfigurationSO audioConfiguration,
            AudioMixerGroup outputAudioMixerGroup,
            Vector3 positionInSpace,
            Transform parent = null)
        {
            if (_musicSoundEmitter && _musicSoundEmitter.IsPlaying())
            {
                AudioClip songToPlay = audioCue.GetClips()[0];

                if (_musicSoundEmitter.GetClip() == songToPlay)
                {
                    return AudioCueKey.Invalid;
                }

                _musicSoundEmitter.FadeOutAudioClip(MUSIC_FADE_DURATION_SECONDS);
            }

            _musicSoundEmitter.FadeInAudioClip(audioCue.GetClips()[0], audioConfiguration, audioCue, outputAudioMixerGroup);
            _musicSoundEmitter.IgnoreListenerPause();

            return AudioCueKey.Invalid;
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

        private AudioCueKey PlayAudioCue(
            AudioCueSO audioCueSO,
            AudioConfigurationSO settings,
            AudioMixerGroup outputAudioMixerGroup,
            Vector3 position = default,
            Transform parent = null)
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
                InitializeSoundEmitter(
                    audioCueSO,
                    settings,
                    outputAudioMixerGroup,
                    key,
                    soundEmitterList,
                    audioClip,
                    parent,
                    position);
            }

            return key;
        }

        private void InitializeSoundEmitter(
            AudioCueSO audioCueSO,
            AudioConfigurationSO settings,
            AudioMixerGroup outputAudioMixerGroup,
            AudioCueKey key,
            List<SoundEmitter> soundEmitterList,
            AudioClip audioClip,
            Transform parent,
            Vector3 position)
        {
            var soundEmitter = GetSoundEmitter();
            soundEmitter.AudioCueKey = key;

            lock (_vaultLock)
            {
                soundEmitterList.Add(soundEmitter);
            }

            if (audioCueSO.FadeIn)
            {
                soundEmitter.FadeInAudioClip(audioClip, settings, audioCueSO, outputAudioMixerGroup, position);
            }
            else
            {
                soundEmitter.Play(audioClip, settings, audioCueSO, outputAudioMixerGroup, position);
            }

            // Pool ownership stays with the manager even when the playback target is disabled or destroyed.
            if (parent)
            {
                soundEmitter.FollowTarget(parent);
            }

            soundEmitter.OnSoundFinishedPlaying += CleanEmitter;
        }

        private bool UpdateAudioCuePlayback(AudioCueKey audioCueKey, float volumeMultiplier01, float pitchMultiplier)
        {
            if (!float.IsFinite(volumeMultiplier01) || !float.IsFinite(pitchMultiplier))
            {
                Echo.Warning("Audio playback update rejected because volume or pitch is not finite.");
                return false;
            }

            lock (_vaultLock)
            {
                if (!_soundEmitterVault.Get(audioCueKey, out List<SoundEmitter> emitters))
                {
                    Echo.Warning("Audio playback update rejected because its playback key is no longer active.");
                    return false;
                }

                foreach (SoundEmitter emitter in emitters)
                    emitter.ApplyPlaybackMultipliers(volumeMultiplier01, pitchMultiplier);
            }

            return true;
        }

        private bool StopAudioCue(AudioCueKey audioCueKey, AudioCueSO audioCue)
        {
            List<SoundEmitter> soundEmitters;
            lock (_vaultLock)
            {
                bool isFound = _soundEmitterVault.Get(audioCueKey, out soundEmitters);
                if (!isFound)
                {
                    return false;
                }
            }

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

            ReleaseSoundEmitter(soundEmitter);
        }

        private void RemoveEmitter(SoundEmitter soundEmitter)
        {
            soundEmitter.OnSoundFinishedPlaying -= CleanEmitter;
            soundEmitter.OnSoundDestroyed -= RemoveEmitter;
            _ownedSoundEmitters.Remove(soundEmitter);
            lock (_vaultLock)
            {
                TryRemoveEmitterFromVault(soundEmitter);
            }

            RemoveEmitterFromPool(soundEmitter);
        }

        private void TryRemoveEmitterFromVault(SoundEmitter soundEmitter)
        {
            if (!_soundEmitterVault.Get(soundEmitter.AudioCueKey, out List<SoundEmitter> soundEmitterList))
            {
                return;
            }

            soundEmitterList.Remove(soundEmitter);

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
            _ownedSoundEmitters.Add(soundEmitter);
            soundEmitter.OnSoundDestroyed += RemoveEmitter;
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

        #endregion
    }
}
