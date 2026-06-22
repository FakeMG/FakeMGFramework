using FakeMG.Framework;
using FakeMG.SaveLoad;
using VContainer;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Bridges tutorial progress into the save system. Captures and restores the
    /// canonical progress held by <see cref="TutorialProgressStore"/>.
    /// </summary>
    public sealed class TutorialProgressSaveable : Saveable
    {
        private TutorialProgressStore _store;

        #region Public Methods

        [Inject]
        public void Construct(TutorialProgressStore store)
        {
            _store = store;
        }

        public override object CaptureState()
        {
            return _store.CaptureSaveData();
        }

        public override void RestoreState(object data)
        {
            if (!StateRestoreUtility.TryRestore(data, out TutorialProgress progress))
            {
                Echo.Warning("Tutorial progress save data is invalid. Restoring default tutorial progress.");
                _store.RestoreDefaultState();
                return;
            }

            _store.RestoreSaveData(progress);
        }

        public override void RestoreDefaultState()
        {
            _store.RestoreDefaultState();
        }

        #endregion
    }
}
