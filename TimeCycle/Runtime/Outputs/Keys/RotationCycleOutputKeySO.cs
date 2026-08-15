using System;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Identifies one rotation cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Time Cycle/Output Keys/Rotation")]
    public sealed class RotationCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(Quaternion);
    }
}
