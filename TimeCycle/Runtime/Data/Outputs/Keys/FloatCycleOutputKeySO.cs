using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Identifies one floating-point cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TIME_CYCLE + "/Output Keys/Float")]
    public sealed class FloatCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(float);
    }
}
