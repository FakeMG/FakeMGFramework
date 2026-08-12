using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Identifies one boolean cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Day Night Cycle/Output Keys/Bool")]
    public sealed class BoolCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(bool);
    }
}
