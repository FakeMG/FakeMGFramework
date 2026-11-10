using UnityEngine;

namespace FakeMG.ItemInteraction
{
    public interface IItemInputLifecycle
    {
        bool CanCommitAtActionMoment(bool isInputHeld);
        bool ShouldCancelWhenReleasedBeforeActionMoment { get; }
        bool IsContinuous { get; }
    }
    public abstract class ItemInputLifecycleSO : ScriptableObject, IItemInputLifecycle
    {
        public abstract bool CanCommitAtActionMoment(bool isInputHeld);
        public abstract bool ShouldCancelWhenReleasedBeforeActionMoment { get; }
        public virtual bool IsContinuous => false;
    }
}
