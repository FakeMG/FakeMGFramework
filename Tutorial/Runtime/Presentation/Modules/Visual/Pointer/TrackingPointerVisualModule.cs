using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    [Serializable]
    public sealed class TrackingPointerVisualModule : TutorialVisualModuleBase<TrackingTutorialPointerView>
    {
        [SerializeField] private TutorialTargetKeySO _targetKey;
        [Tooltip("Where on the target the fingertip rests, normalized over its rect: (0.5, 0.5) is the center, " +
                 "(1, 0) the bottom-right corner. Off-center points keep the hand off the target's label.")]
        [SerializeField] private Vector2 _pointOnTarget01 = new(0.5f, 0.5f);

        protected override bool ConfigureView(TrackingTutorialPointerView view, TutorialContext context)
        {
            view.Track(new UiTargetPointerAnchor(context.TargetRegistry, _targetKey, _pointOnTarget01));
            return true;
        }
    }
}
