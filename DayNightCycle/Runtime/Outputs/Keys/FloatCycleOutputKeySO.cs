using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Identifies one floating-point cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Day Night Cycle/Output Keys/Float")]
    public sealed class FloatCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(float);
    }
}
