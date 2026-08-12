using System;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Identifies one color cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = "FakeMG/Day Night Cycle/Output Keys/Color")]
    public sealed class ColorCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(Color);
    }
}
