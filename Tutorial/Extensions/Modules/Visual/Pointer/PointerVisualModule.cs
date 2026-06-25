using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Shows a pointer aimed at a UI target resolved by key, so it can point at runtime UI
    /// such as a button inside a popup. The pointer hides as soon as the player interacts
    /// with the target, whichever click source the target exposes.
    /// </summary>
    [Serializable]
    public sealed class PointerVisualModule : TutorialVisualModuleBase<TutorialPointerView>
    {
        [SerializeField] private TutorialTargetKeySO _targetKey;

        protected override void ConfigureView(TutorialPointerView view, TutorialContext context)
        {
            view.PointAt(ResolveTargetRect(context, _targetKey), context.VisualRoot);
        }
    }
}
