using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Identifies one integer cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Day Night Cycle/Output Keys/Int")]
    public sealed class IntCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(int);
    }
}
