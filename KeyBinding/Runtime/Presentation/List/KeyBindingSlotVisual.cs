using UnityEngine;

namespace FakeMG.KeyBinding
{
    /// <summary>How a binding slot looks in each state. Replace it to restyle slots without touching their logic.</summary>
    public abstract class KeyBindingSlotVisual : MonoBehaviour
    {
        public abstract void ShowIdle();
        public abstract void ShowRebinding();
        public abstract void ShowKeyChanged();

        /// <summary>Jumps to the idle look with no motion, e.g. when the slot is hidden or shows another binding.</summary>
        public abstract void SnapToIdle();
    }
}
