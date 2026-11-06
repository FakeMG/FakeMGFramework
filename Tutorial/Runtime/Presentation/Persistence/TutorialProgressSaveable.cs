using FakeMG.SaveLoad;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Bridges tutorial progress into the save system. Captures and restores the
    /// canonical progress held by <see cref="TutorialProgressStore"/>.
    /// </summary>
    public sealed class TutorialProgressSaveable : ISaveable
    {
        public const string SAVE_ID = nameof(TutorialProgressSaveable);

        private readonly TutorialProgressStore _store;

        public string SaveId => SAVE_ID;

        public TutorialProgressSaveable(TutorialProgressStore store)
        {
            _store = store;
        }

        #region Public Methods

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
