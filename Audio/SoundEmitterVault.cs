using System;
using System.Collections.Generic;

namespace FakeMG.FakeMGFramework.Audio
{
    public class SoundEmitterVault
    {
        private readonly Dictionary<AudioCueKey, List<SoundEmitter>> _emitters = new();

        private AudioCueKey GetKey(AudioCueSO cue)
        {
            string uniqueKey = Guid.NewGuid().ToString();
            return new AudioCueKey(uniqueKey, cue);
        }

        public AudioCueKey Add(AudioCueSO cue, List<SoundEmitter> soundEmitterList)
        {
            AudioCueKey emitterKey = GetKey(cue);

            _emitters.Add(emitterKey, soundEmitterList);

            return emitterKey;
        }

        public bool Get(AudioCueKey key, out List<SoundEmitter> soundEmitterList)
        {
            return _emitters.TryGetValue(key, out soundEmitterList);
        }

        public bool Remove(AudioCueKey key)
        {
            return _emitters.Remove(key);
        }
    }
}