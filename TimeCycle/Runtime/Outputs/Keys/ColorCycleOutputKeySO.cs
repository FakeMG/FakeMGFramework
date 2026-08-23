using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Identifies one color cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TIME_CYCLE + "/Output Keys/Color")]
    public sealed class ColorCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(Color);
    }
}
