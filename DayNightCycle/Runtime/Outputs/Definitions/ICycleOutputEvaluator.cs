using System;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Evaluates one configured output at a presentation time within the cycle.
    /// </summary>
    public interface ICycleOutputEvaluator
    {
        Type ValueType { get; }
        object Evaluate(double presentationTimeSeconds);
    }
}
