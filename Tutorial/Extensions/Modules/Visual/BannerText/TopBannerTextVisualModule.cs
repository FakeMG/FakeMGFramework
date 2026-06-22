using System;
using UnityEngine;

namespace FakeMG.Tutorial
{
    [Serializable]
    public sealed class TopBannerTextVisualModule : TutorialVisualModuleBase<TopBannerTextView>
    {
        [SerializeField, TextArea] private string _instruction;

        protected override void ConfigureView(TopBannerTextView view, TutorialContext context)
        {
            view.SetInstruction(_instruction);
        }
    }
}
