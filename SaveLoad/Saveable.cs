using UnityEngine;

namespace FakeMG.Framework.SaveLoad
{
    public abstract class Saveable : MonoBehaviour
    {
        public string GetUniqueId()
        {
            return GetType().ToString();
        }

        public abstract object CaptureState();
        public abstract void RestoreState(object data);
        public abstract void RestoreDefaultState();
    }
}