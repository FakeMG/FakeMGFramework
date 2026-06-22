using FakeMG.Framework.EventBus;

namespace FakeMG.Tutorial
{
    public struct TutorialStartedEvent : IEvent
    {
        public TutorialId TutorialId;
        public bool IsForcedReplay;
    }

    public struct TutorialStepStartedEvent : IEvent
    {
        public TutorialId TutorialId;
        public StepId StepId;
    }

    public struct TutorialStepCompletedEvent : IEvent
    {
        public TutorialId TutorialId;
        public StepId StepId;
    }

    public struct TutorialStepSkippedEvent : IEvent
    {
        public TutorialId TutorialId;
        public StepId StepId;
        public SkipReason Reason;
    }

    public struct TutorialBranchChosenEvent : IEvent
    {
        public TutorialId TutorialId;
        public StepId FromStepId;
        public StepId BranchStepId;
    }

    public struct TutorialEndedEvent : IEvent
    {
        public TutorialId TutorialId;
        public bool ReachedValidEndState;
        public bool IsForcedReplay;
    }
}
