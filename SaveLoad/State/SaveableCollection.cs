using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    public static class SaveableCollection
    {
        #region Public Methods

        public static IReadOnlyDictionary<string, object> Capture(IReadOnlyDictionary<string, ISaveable> saveables)
        {
            Dictionary<string, object> capturedStates = new(saveables.Count);
            foreach (KeyValuePair<string, ISaveable> saveable in saveables)
            {
                capturedStates.Add(saveable.Key, saveable.Value.CaptureState());
            }

            return capturedStates;
        }

        #endregion
    }
}
