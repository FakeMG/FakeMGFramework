using System;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Identifies one boolean cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Time Cycle/Output Keys/Bool")]
    public sealed class BoolCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(bool);
    }
}
