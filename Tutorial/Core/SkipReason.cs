namespace FakeMG.Tutorial
{
    public enum SkipReason
    {
        None = 0,
        MissingTarget,
        RequiredDependencyFailed,
        RequiredVisualLoadFailed,
        NotInChosenBranch,
        AlreadySatisfied,
        OptionalBySystemLogic
    }
}
