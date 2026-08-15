using System;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Identifies one color cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Time Cycle/Output Keys/Color")]
    public sealed class ColorCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(Color);
    }
}
