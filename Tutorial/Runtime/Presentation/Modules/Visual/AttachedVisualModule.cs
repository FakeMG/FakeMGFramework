using System;

namespace FakeMG.Tutorial
{
    [Serializable]
    public sealed class AttachedVisualModule : TutorialVisualModuleBase<AnimatedTutorialVisualView>
    {
        protected override bool ConfigureView(AnimatedTutorialVisualView view, TutorialContext context) => true;
    }
}
