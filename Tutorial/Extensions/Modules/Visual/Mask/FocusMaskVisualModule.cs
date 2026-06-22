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

        protected override void ConfigureView(TutorialFramingVisualView view, TutorialContext context)
        {
            view.Frame(ResolveTargetRect(context, _focusTargetKey));
        }
    }
}
