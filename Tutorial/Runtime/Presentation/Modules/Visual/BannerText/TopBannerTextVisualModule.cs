using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    [Serializable]
    public sealed class TopBannerTextVisualModule : TutorialVisualModuleBase<TopBannerTextView>
    {
        [SerializeField, TextArea] private string _instruction;

        protected override bool ConfigureView(TopBannerTextView view, TutorialContext context)
        {
            view.SetInstruction(_instruction);
            return true;
        }
    }
}
