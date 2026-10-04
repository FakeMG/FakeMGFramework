using FakeMG.SaveLoad;
using UnityEngine;
using VContainer;

namespace FakeMG.TimeCycle
{
    public sealed class WorldTimelineSaveable : MonoBehaviour, ISaveable
    {
        public const string SAVE_ID = "WorldTimeline";

        private WorldTimelinePersistence _persistence;

        public string SaveId => SAVE_ID;

        #region Public Methods

        [Inject]
        public void Construct(WorldTimelinePersistence persistence) => _persistence = persistence;

        public object CaptureState() => _persistence.CaptureForStorage();

        public bool TryValidateState(object state, out string failureReason) => WorldTimelinePersistence.TryValidate(state, out failureReason);

        public void RestoreState(object state) => _persistence.Restore((WorldTimelineSaveData)state);

        public void RestoreDefaultState() => _persistence.Reset();

        #endregion
    }
}
