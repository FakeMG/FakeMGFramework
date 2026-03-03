using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

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

        private Queue<SoundEmitter> _soundEmitterQueue;
        private SoundEmitterVault _soundEmitterVault;
        private readonly object _vaultLock = new();
        private readonly HashSet<AudioCueEventChannelSO> _registeredPooledChannels = new();

        private void Awake()
        {
            _soundEmitterQueue = new Queue<SoundEmitter>();
            _soundEmitterVault = new SoundEmitterVault();
        }

        private void OnEnable()
        {
            RegisterPooledChannels();
            RegisterMusicChannel();
        }

        private void Start()
        {
            InitializeConfiguredVolumeChannels();
        }

        private void OnDisable()
        {
            UnregisterPooledChannels();
            UnregisterMusicChannel();

            CleanPool();

            if (_musicSoundEmitter && _musicSoundEmitter.IsPlaying())
            {
                _musicSoundEmitter.Stop();
            }
        }

        private void OnValidate()
        {
            ValidatePooledChannelConfiguration();
        }

        private void RegisterPooledChannels()
        {
            _registeredPooledChannels.Clear();
            if (_pooledEventChannels == null)
            {
                return;
            }

            foreach (var pooledChannel in _pooledEventChannels)
            {
                if (!CanRegisterPooledChannel(pooledChannel))
                {
                    continue;
                }

                pooledChannel.OnAudioCuePlayRequested += PlayAudioCue;
                pooledChannel.OnAudioCueStopRequested += StopAudioCue;
                pooledChannel.OnAudioCueFinishRequested += FinishAudioCue;
                _registeredPooledChannels.Add(pooledChannel);
            }
        }

        private bool CanRegisterPooledChannel(AudioCueEventChannelSO pooledChannel)
        {
            if (!pooledChannel)
            {
                Debug.LogError("A pooled audio event channel reference is missing.", this);
                return false;
            }

            if (pooledChannel == _musicEventChannel)
            {
                Debug.LogError("The music channel cannot also be registered as a pooled channel.", this);
                return false;
            }

            if (_registeredPooledChannels.Contains(pooledChannel))
            {
                Debug.LogError($"Pooled channel '{pooledChannel.name}' is duplicated.", this);
                return false;
            }

            return true;
        }

        private void UnregisterPooledChannels()
        {
            foreach (var pooledChannel in _registeredPooledChannels)
            {
                pooledChannel.OnAudioCuePlayRequested -= PlayAudioCue;
                pooledChannel.OnAudioCueStopRequested -= StopAudioCue;
                pooledChannel.OnAudioCueFinishRequested -= FinishAudioCue;
            }

            _registeredPooledChannels.Clear();
        }

        private void RegisterMusicChannel()
        {
            _musicEventChannel.OnAudioCuePlayRequested += PlayMusicTrack;
            _musicEventChannel.OnAudioCueStopRequested += StopMusicTrack;
        }

        private void UnregisterMusicChannel()
        {
            _musicEventChannel.OnAudioCuePlayRequested -= PlayMusicTrack;
            _musicEventChannel.OnAudioCueStopRequested -= StopMusicTrack;
        }

        private void ValidatePooledChannelConfiguration()
        {
            if (_pooledEventChannels == null)
            {
                return;
            }

            var uniqueChannels = new HashSet<AudioCueEventChannelSO>();

            foreach (var pooledChannel in _pooledEventChannels)
            {
                if (!pooledChannel)
                {
                    Debug.LogError("A pooled audio event channel reference is missing.", this);
                    continue;
                }

                if (pooledChannel == _musicEventChannel)
                {
                    Debug.LogError("The music channel cannot also be in pooled channels.", this);
                    continue;
                }

                if (!uniqueChannels.Add(pooledChannel))
                {
                    Debug.LogError($"Pooled channel '{pooledChannel.name}' is duplicated.", this);
                }
            }
        }

        private void InitializeConfiguredVolumeChannels()
        {
            var initializedChannels = new HashSet<AudioCueEventChannelSO>();
            InitializeVolumeForChannel(_musicEventChannel, initializedChannels);

            if (_pooledEventChannels == null)
            {
                return;
            }

            foreach (var pooledChannel in _pooledEventChannels)
            {
                InitializeVolumeForChannel(pooledChannel, initializedChannels);
            }
        }

        private void InitializeVolumeForChannel(AudioCueEventChannelSO eventChannel, HashSet<AudioCueEventChannelSO> initializedChannels)
        {
            if (!eventChannel || !initializedChannels.Add(eventChannel))
            {
                return;
            }

            eventChannel.InitializePersistedVolume();
        }

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

            if (parent)
            {
                soundEmitter.transform.SetParent(parent);
            }

            if (audioCueSO.FadeIn)
            {
                soundEmitter.FadeInAudioClip(audioClip, settings, audioCueSO, outputAudioMixerGroup);
            }
            else
            {
                soundEmitter.Play(audioClip, settings, audioCueSO, outputAudioMixerGroup, position);
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
            if (_soundEmitterQueue == null)
            {
                return;
            }

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
