namespace FakeMG.Tutorial
{
    /// <summary>
    /// Provides the ordered and branchable steps of a single tutorial and resolves
    /// which step runs next given progress and the previous step's result. Returning
    /// null from a Get*Step method signals there is no further valid step.
    /// </summary>
    public interface ITutorialSequence
    {
        TutorialId Id { get; }
        ITutorialStep GetFirstStep(TutorialProgress progress);
        ITutorialStep GetNextStep(StepId currentStepId, TutorialStepResult lastResult, TutorialProgress progress);
        bool IsValidEndState(StepId lastStepId, TutorialProgress progress);
    }
}
