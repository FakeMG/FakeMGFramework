using System;

namespace FakeMG.TimeCycle
{
    public interface ICycleOutputEvaluator
    {
        Type ValueType { get; }
        object Evaluate(double presentationTimeSeconds);
    }
}
