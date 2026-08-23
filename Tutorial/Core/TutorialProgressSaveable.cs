using FakeMG.Framework;
using FakeMG.SaveLoad;
using UnityEngine;
using VContainer;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Bridges tutorial progress into the save system. Captures and restores the
    /// canonical progress held by <see cref="TutorialProgressStore"/>.
    /// </summary>
    public sealed class TutorialProgressSaveable : MonoBehaviour, ISaveable
    {
        public const string SAVE_ID = nameof(TutorialProgressSaveable);

        private TutorialProgressStore _store;

        public string SaveId => SAVE_ID;

        #region Public Methods

        [Inject]
        public void Construct(TutorialProgressStore store)
        {
            _store = store;
        }

        public object CaptureState()
        {
            return _store.CaptureSaveData();
        }

        public bool TryValidateState(object state, out string failureReason)
        {
            if (state is TutorialProgress)
            {
                failureReason = string.Empty;
                return true;
            }

            failureReason = "Tutorial progress state is invalid.";
            return false;
        }

        public void RestoreState(object data)
        {
            _store.RestoreSaveData((TutorialProgress)data);
        }

        public void RestoreDefaultState()
        {
            _store.RestoreDefaultState();
        }

        #endregion
    }
}
