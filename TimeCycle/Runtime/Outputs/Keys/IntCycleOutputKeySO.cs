using System;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Identifies one integer cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Time Cycle/Output Keys/Int")]
    public sealed class IntCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(int);
    }
}
