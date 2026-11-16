using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    [Serializable]
    public sealed class FocusMaskVisualModule : TutorialVisualModuleBase<TutorialFramingVisualView>
    {
        [SerializeField] private TutorialTargetKeySO _focusTargetKey;

        protected override bool ConfigureView(TutorialFramingVisualView view, TutorialContext context)
        {
            if (!TryResolveTargetRect(context, _focusTargetKey, out RectTransform focusRect))
            {
                return false;
            }

            view.Frame(focusRect);
            return true;
        }
    }
}
