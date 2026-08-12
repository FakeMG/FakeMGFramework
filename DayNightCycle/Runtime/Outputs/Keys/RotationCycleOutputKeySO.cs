using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Identifies one rotation cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Day Night Cycle/Output Keys/Rotation")]
    public sealed class RotationCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(Quaternion);
    }
}
