using UnityEngine;

namespace FakeMG.FakeMGFramework.Audio
{
    [CreateAssetMenu(menuName = "Audio/AudioCueEventChannel")]
    public class AudioCueEventChannelSO : ScriptableObject
    {
        public event AudioCuePlayAction OnAudioCuePlayRequested;
        public event AudioCueStopAction OnAudioCueStopRequested;
        public event AudioCueFinishAction OnAudioCueFinishRequested;
        
        public AudioCueKey RaisePlayEvent(
            AudioCueSO audioCue, AudioConfigurationSO audioConfiguration,
            Vector3 positionInSpace = default, Transform parent = null)
        {
            AudioCueKey audioCueKey = AudioCueKey.Invalid;

            if (OnAudioCuePlayRequested != null)
            {
                audioCueKey = OnAudioCuePlayRequested.Invoke(audioCue, audioConfiguration, positionInSpace, parent);
            }
            else
            {
                Debug.LogWarning("An AudioCue play event was requested  for " + audioCue.name +
                                 ", but nobody picked it up. " +
                                 "Check why there is no AudioManager already loaded, " +
                                 "and make sure it's listening on this AudioCue Event channel.");
            }

            return audioCueKey;
        }

        public bool RaiseStopEvent(AudioCueKey audioCueKey, AudioCueSO audioCue)
        {
            bool requestSucceed = false;

            if (OnAudioCueStopRequested != null)
            {
                requestSucceed = OnAudioCueStopRequested.Invoke(audioCueKey, audioCue);
            }
            else
            {
                Debug.LogWarning("An AudioCue stop event was requested, but nobody picked it up. " +
                                 "Check why there is no AudioManager already loaded, " +
                                 "and make sure it's listening on this AudioCue Event channel.");
            }

            return requestSucceed;
        }

        public bool RaiseFinishEvent(AudioCueKey audioCueKey)
        {
            bool requestSucceed = false;

            if (OnAudioCueFinishRequested != null)
            {
                requestSucceed = OnAudioCueFinishRequested.Invoke(audioCueKey);
            }
            else
            {
                Debug.LogWarning("An AudioCue finish event was requested, but nobody picked it up. " +
                                 "Check why there is no AudioManager already loaded, " +
                                 "and make sure it's listening on this AudioCue Event channel.");
            }

            return requestSucceed;
        }
    }

    public delegate AudioCueKey AudioCuePlayAction(
        AudioCueSO audioCue, AudioConfigurationSO audioConfiguration,
        Vector3 positionInSpace, Transform parent);

    public delegate bool AudioCueStopAction(AudioCueKey emitterKey, AudioCueSO audioCue);

    public delegate bool AudioCueFinishAction(AudioCueKey emitterKey);
}