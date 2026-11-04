using FakeMG.Audio;
using UnityEngine;

namespace FakeMG.Inventory.Hud
{
    [DisallowMultipleComponent]
    public sealed class ItemCounterRewardAudioSubscriber : MonoBehaviour
    {
        [SerializeField] private ItemCounterView _counter;
        [SerializeField] private PitchRisingAudioCue _arrivalAudio;

        #region Unity Lifecycle

        private void OnEnable()
        {
            _counter.OnRewardTokenArrived += PlayArrivalAudio;
        }

        private void OnDisable()
        {
            _counter.OnRewardTokenArrived -= PlayArrivalAudio;
        }

        #endregion

        #region Private Methods

        private void PlayArrivalAudio()
        {
            _arrivalAudio.PlayAudioCue();
        }

        #endregion
    }
}
