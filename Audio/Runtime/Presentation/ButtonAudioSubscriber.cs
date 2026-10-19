using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class ButtonAudioSubscriber : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private AudioCue _audioCue;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _button.onClick.AddListener(PlayClickAudio);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(PlayClickAudio);
        }

        #endregion

        #region Private Methods

        private void PlayClickAudio()
        {
            _audioCue.PlayAudioCue();
        }

        #endregion
    }
}
