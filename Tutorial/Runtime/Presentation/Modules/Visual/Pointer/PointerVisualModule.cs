using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    [Serializable]
    public sealed class PointerVisualModule : TutorialVisualModuleBase<TutorialPointerView>
    {
        [SerializeField] private TutorialTargetKeySO _targetKey;

        protected override bool ConfigureView(TutorialPointerView view, TutorialContext context)
        {
            if (!TryResolveTargetRect(context, _targetKey, out RectTransform targetRect))
            {
                return false;
            }

            view.PointAt(targetRect, ParentRect);
            return true;
        }
    }
}
