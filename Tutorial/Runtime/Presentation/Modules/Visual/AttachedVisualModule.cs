using System;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Shows a self-contained visual (a notification badge, a glow) with no content to configure.
    /// Set the parent target key to attach it to a UI target so it follows that target's layout.
    /// </summary>
    [Serializable]
    public sealed class AttachedVisualModule : TutorialVisualModuleBase<AnimatedTutorialVisualView>
    {
        protected override bool ConfigureView(AnimatedTutorialVisualView view, TutorialContext context) => true;
    }
}
