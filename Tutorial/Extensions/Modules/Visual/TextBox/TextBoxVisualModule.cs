using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Shows an instruction text box positioned near a UI target resolved by key, so it
    /// can anchor to runtime UI. Localization is out of scope; the raw string is passed
    /// straight to TextMeshPro.
    /// </summary>
    [Serializable]
    public sealed class TextBoxVisualModule : TutorialVisualModuleBase<TutorialTextBoxView>
    {
        [TextArea]
        [SerializeField] private string _instruction;
        [SerializeField] private TutorialTargetKeySO _targetKey;

        protected override bool ConfigureView(TutorialTextBoxView view, TutorialContext context)
        {
            if (!TryResolveTargetRect(context, _targetKey, out RectTransform targetRect))
            {
                return false;
            }

            view.SetInstruction(_instruction);
            view.PositionNear(targetRect);
            return true;
        }
    }
}
