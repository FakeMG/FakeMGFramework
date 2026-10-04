using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Dims the screen and frames a UI target resolved by key to focus attention on it
    /// (focus mask / highlight). The framing view supplies the black background.
    /// </summary>
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
