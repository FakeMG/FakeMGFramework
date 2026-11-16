using UnityEngine;

namespace FakeMG.Tutorial
{
    public sealed class TutorialContext
    {
        public TutorialContext(TutorialId tutorialId, TutorialProgress progress, TutorialTargetRegistry targetRegistry,
            TutorialInteractionGate gate, TutorialAddressableLoader loader,
            RectTransform visualRoot, RectTransform hiddenUIRoot, TutorialForceCompleteSignal forceComplete)
        {
            TutorialId = tutorialId;
            Progress = progress;
            TargetRegistry = targetRegistry;
            Gate = gate;
            Loader = loader;
            VisualRoot = visualRoot;
            HiddenUIRoot = hiddenUIRoot;
            ForceComplete = forceComplete;
        }

        public TutorialId TutorialId { get; }
        public TutorialProgress Progress { get; }
        public TutorialTargetRegistry TargetRegistry { get; }
        public TutorialInteractionGate Gate { get; }
        public TutorialAddressableLoader Loader { get; }
        public RectTransform VisualRoot { get; }

        /// <summary>
        /// The screen-space HUD canvas a step's behavior modules act on (e.g. to hide
        /// gameplay buttons). Null when the gameplay scope has no registered HUD root.
        /// </summary>
        public RectTransform HiddenUIRoot { get; }

        public TutorialForceCompleteSignal ForceComplete { get; }
    }
}
