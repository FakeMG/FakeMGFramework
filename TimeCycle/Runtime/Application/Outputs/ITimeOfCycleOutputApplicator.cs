using System.Collections.Generic;

namespace FakeMG.TimeCycle
{
    public interface ITimeOfCycleOutputApplicator
    {
        IReadOnlyList<CycleOutputKeySO> RequiredOutputKeys { get; }
        void Apply(IReadOnlyCycleOutputState outputState);
    }
}
