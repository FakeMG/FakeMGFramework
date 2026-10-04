namespace FakeMG.Tutorial
{
    /// <summary>
    /// Owns the canonical saved tutorial progress and hands the runner the progress to
    /// write into for a session. A normal session writes to saved progress; a forced
    /// replay writes to a throwaway copy that is discarded, so saved progress is never
    /// changed by a replay. Debug force-complete writes straight to saved.
    /// </summary>
    public sealed class TutorialProgressStore
    {
        private TutorialProgress _saved = new();

        public TutorialProgress Saved => _saved;

        public TutorialProgress BeginNormalSession() => _saved;

        public TutorialProgress BeginReplaySession() => new();

        public void ForceCompleteStep(TutorialId tutorialId, StepId stepId) =>
            _saved.MarkStepCompleted(tutorialId, stepId);

        public void ResetTutorial(TutorialId tutorialId) => _saved.ClearTutorial(tutorialId);

        public void ResetAll() => _saved.Clear();

        public TutorialProgress CaptureSaveData() => _saved.DeepCopy();

        public void RestoreSaveData(TutorialProgress data) => _saved = data ?? new TutorialProgress();

        public void RestoreDefaultState() => _saved = new TutorialProgress();
    }
}
