#if UNITY_EDITOR
using UnityEngine;

namespace FakeMG.Framework.SaveLoad.Examples
{
    /// <summary>
    /// Example Saveable that would be in the Manager scene
    /// It registers itself in the SOReference so systems can find it
    /// </summary>
    public class ExamplePlayerDataSaveable : Saveable
    {
        [SerializeField] private ExamplePlayerDataSaveableReference _selfReference;

        protected void Start()
        {
            if (_selfReference)
            {
                _selfReference.Set(this);
            }
        }

        public override object CaptureState()
        {
            // Convert the actual data to the saved data type
            return new PlayerSaveData();
        }

        public override void RestoreState(object data)
        {
            if (data is PlayerSaveData restoredData)
            {
                // Convert the saved data to the actual data type
            }
        }

        public override void RestoreDefaultState()
        {
            // Restore the default state
        }
    }
}
#endif