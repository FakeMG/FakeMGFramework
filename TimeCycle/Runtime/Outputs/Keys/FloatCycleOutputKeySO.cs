using System;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Identifies one floating-point cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Time Cycle/Output Keys/Float")]
    public sealed class FloatCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(float);
    }
}
