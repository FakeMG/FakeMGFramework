using FakeMG.SaveLoad;

namespace FakeMG.TimeCycle
{
    public sealed class WorldTimelineSaveable : ISaveable
    {
        public const string SAVE_ID = "WorldTimeline";

        private readonly WorldTimelinePersistence _persistence;

        public string SaveId => SAVE_ID;

        public WorldTimelineSaveable(WorldTimelinePersistence persistence) => _persistence = persistence;

        #region Public Methods

        public object CaptureState() => _persistence.CaptureForStorage();

        public bool TryValidateState(object state, out string failureReason) => WorldTimelinePersistence.TryValidate(state, out failureReason);

        public void RestoreState(object state) => _persistence.Restore((WorldTimelineSaveData)state);

        public void RestoreDefaultState() => _persistence.Reset();

        #endregion
    }
}
