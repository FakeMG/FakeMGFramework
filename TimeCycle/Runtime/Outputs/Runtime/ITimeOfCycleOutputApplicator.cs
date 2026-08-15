using System.Collections.Generic;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Declares required output keys and applies evaluated values to one concrete consumer.
    /// </summary>
    public interface ITimeOfCycleOutputApplicator
    {
        IReadOnlyList<CycleOutputKeySO> RequiredOutputKeys { get; }
        void Apply(IReadOnlyCycleOutputState outputState);
    }
}
