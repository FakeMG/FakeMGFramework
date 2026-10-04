namespace FakeMG.Tutorial
{
    public readonly struct TutorialStepResult
    {
        public TutorialStepOutcome Outcome { get; }
        public SkipReason SkipReason { get; }
        public StepId ChosenBranch { get; }
        public bool HasBranch { get; }

        private TutorialStepResult(TutorialStepOutcome outcome, SkipReason skipReason, StepId chosenBranch, bool hasBranch)
        {
            Outcome = outcome;
            SkipReason = skipReason;
            ChosenBranch = chosenBranch;
            HasBranch = hasBranch;
        }

        public static TutorialStepResult Completed() =>
            new(TutorialStepOutcome.Completed, SkipReason.None, default, false);

        public static TutorialStepResult CompletedWithBranch(StepId branch) =>
            new(TutorialStepOutcome.Completed, SkipReason.None, branch, true);

        public static TutorialStepResult Skipped(SkipReason reason) =>
            new(TutorialStepOutcome.Skipped, reason, default, false);
    }
}
