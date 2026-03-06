using System.Collections.Generic;
using FakeMG.Framework;
using FakeMG.Settings;
using UnityEngine;

namespace FakeMG.Audio
{
    public class AudioChannelRegistry
    {
        private const float MIN_LINEAR_VOLUME = 0.0001f;
        private const float DECIBEL_SCALE = 20f;

        private readonly List<AudioCueEventChannelSO> _pooledChannels;
        private readonly AudioCueEventChannelSO _musicChannel;
        private readonly HashSet<AudioCueEventChannelSO> _registeredPooledChannels = new();
        private readonly Dictionary<string, AudioCueEventChannelSO> _settingIdToChannel = new();
        private HashSet<AudioCueEventChannelSO> _subscribedVolumeChannels = new();

        private AudioCuePlayAction _pooledPlayHandler;
        private AudioCueStopAction _pooledStopHandler;
        private AudioCueFinishAction _pooledFinishHandler;
        private AudioCuePlayAction _musicPlayHandler;
        private AudioCueStopAction _musicStopHandler;

        public AudioChannelRegistry(
            List<AudioCueEventChannelSO> pooledChannels,
            AudioCueEventChannelSO musicChannel)
        {
            _pooledChannels = pooledChannels;
            _musicChannel = musicChannel;
            BuildSettingToChannelMap();
        }

        public void RegisterPooledChannels(
            AudioCuePlayAction playHandler,
            AudioCueStopAction stopHandler,
            AudioCueFinishAction finishHandler)
        {
            _pooledPlayHandler = playHandler;
            _pooledStopHandler = stopHandler;
            _pooledFinishHandler = finishHandler;

            _registeredPooledChannels.Clear();
            if (_pooledChannels == null)
            {
                return;
            }

            foreach (var channel in _pooledChannels)
            {
                if (!CanRegisterPooledChannel(channel))
                {
                    continue;
                }

                channel.OnAudioCuePlayRequested += playHandler;
                channel.OnAudioCueStopRequested += stopHandler;
                channel.OnAudioCueFinishRequested += finishHandler;
                _registeredPooledChannels.Add(channel);
            }
        }

        public void UnregisterPooledChannels()
        {
            foreach (var channel in _registeredPooledChannels)
            {
                channel.OnAudioCuePlayRequested -= _pooledPlayHandler;
                channel.OnAudioCueStopRequested -= _pooledStopHandler;
                channel.OnAudioCueFinishRequested -= _pooledFinishHandler;
            }

            _registeredPooledChannels.Clear();
        }

        public void RegisterMusicChannel(
            AudioCuePlayAction playHandler,
            AudioCueStopAction stopHandler)
        {
            _musicPlayHandler = playHandler;
            _musicStopHandler = stopHandler;

            _musicChannel.OnAudioCuePlayRequested += playHandler;
            _musicChannel.OnAudioCueStopRequested += stopHandler;
        }

        public void UnregisterMusicChannel()
        {
            _musicChannel.OnAudioCuePlayRequested -= _musicPlayHandler;
            _musicChannel.OnAudioCueStopRequested -= _musicStopHandler;
        }

        public void SubscribeToVolumeChanges(SettingDataManager settingDataManager)
        {
            _subscribedVolumeChannels = GetUniqueVolumeChannels();

            foreach (var channel in _subscribedVolumeChannels)
            {
                if (!channel.VolumeSliderSetting)
                {
                    Echo.Error($"Channel '{channel.name}' is missing a reference to a volume slider setting, " +
                                   "but it was included in the unique volume channels list. " +
                                   "Check the channel configuration and make sure it has a valid volume slider setting assigned.");
                    continue;
                }
                settingDataManager.Subscribe(channel.VolumeSliderSetting, ApplyVolumeWhenSettingChanged);
            }
        }

        public void UnsubscribeFromVolumeChanges(SettingDataManager settingDataManager)
        {
            foreach (var channel in _subscribedVolumeChannels)
            {
                if (!channel.VolumeSliderSetting)
                {
                    Echo.Error($"Channel '{channel.name}' is missing a reference to a volume slider setting, " +
                                   "but it was included in the unique volume channels list. " +
                                   "Check the channel configuration and make sure it has a valid volume slider setting assigned.");
                    continue;
                }
                settingDataManager.Unsubscribe(channel.VolumeSliderSetting, ApplyVolumeWhenSettingChanged);
            }

            _subscribedVolumeChannels.Clear();
        }

        private void ApplyVolumeWhenSettingChanged(SettingDataGeneric<float> setting, float volume)
        {
            if (!_settingIdToChannel.TryGetValue(setting.SettingId, out AudioCueEventChannelSO channel))
            {
                return;
            }

            ApplyVolumeToChannel(channel, volume);
        }

        public void InitializeChannelVolumes(SettingDataManager settingDataManager)
        {
            foreach (var channel in _subscribedVolumeChannels)
            {
                float persistedVolume = settingDataManager.GetValue(channel.VolumeSliderSetting);
                ApplyVolumeToChannel(channel, persistedVolume);
            }
        }

        private void ApplyVolumeToChannel(AudioCueEventChannelSO channel, float volume)
        {
            if (!channel.HasVolumeConfiguration)
            {
                return;
            }

            float clampedVolume = Mathf.Clamp01(volume);
            float stableLinearVolume = Mathf.Max(clampedVolume, MIN_LINEAR_VOLUME);
            float decibelValue = Mathf.Log10(stableLinearVolume) * DECIBEL_SCALE;

            bool volumeSet = channel.AudioMixer.SetFloat(channel.MixerVolumeParameter, decibelValue);
            if (!volumeSet)
            {
                Echo.Error($"The AudioMixer parameter '{channel.MixerVolumeParameter}' was not found.");
            }
        }

        public static void ValidatePooledChannelConfiguration(
            List<AudioCueEventChannelSO> pooledChannels,
            AudioCueEventChannelSO musicChannel)
        {
            if (pooledChannels == null)
            {
                return;
            }

            var uniqueChannels = new HashSet<AudioCueEventChannelSO>();

            foreach (var channel in pooledChannels)
            {
                if (!channel)
                {
                    Echo.Error("A pooled audio event channel reference is missing.");
                    continue;
                }

                if (channel == musicChannel)
                {
                    Echo.Error("The music channel cannot also be in pooled channels.");
                    continue;
                }

                if (!uniqueChannels.Add(channel))
                {
                    Echo.Error($"Pooled channel '{channel.name}' is duplicated.");
                }
            }
        }

        private bool CanRegisterPooledChannel(AudioCueEventChannelSO channel)
        {
            if (!channel)
            {
                Echo.Error("A pooled audio event channel reference is missing.");
                return false;
            }

            if (channel == _musicChannel)
            {
                Echo.Error("The music channel cannot also be registered as a pooled channel.");
                return false;
            }

            if (_registeredPooledChannels.Contains(channel))
            {
                Echo.Error($"Pooled channel '{channel.name}' is duplicated.");
                return false;
            }

            return true;
        }

        private HashSet<AudioCueEventChannelSO> GetUniqueVolumeChannels()
        {
            var channels = new HashSet<AudioCueEventChannelSO>();

            if (_musicChannel && _musicChannel.HasVolumeConfiguration)
            {
                channels.Add(_musicChannel);
            }

            if (_pooledChannels == null)
            {
                return channels;
            }

            foreach (var channel in _pooledChannels)
            {
                if (channel && channel.HasVolumeConfiguration)
                {
                    channels.Add(channel);
                }
            }

            return channels;
        }

        private void BuildSettingToChannelMap()
        {
            TryAddChannelToMap(_musicChannel);

            if (_pooledChannels == null)
            {
                return;
            }

            foreach (var channel in _pooledChannels)
            {
                TryAddChannelToMap(channel);
            }
        }

        private void TryAddChannelToMap(AudioCueEventChannelSO channel)
        {
            if (!channel || !channel.VolumeSliderSetting)
            {
                return;
            }

            _settingIdToChannel.TryAdd(channel.VolumeSliderSetting.SettingId, channel);
        }
    }
}