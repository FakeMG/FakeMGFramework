using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
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
