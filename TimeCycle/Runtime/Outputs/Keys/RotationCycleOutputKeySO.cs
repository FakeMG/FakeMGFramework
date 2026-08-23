using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Identifies one rotation cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TIME_CYCLE + "/Output Keys/Rotation")]
    public sealed class RotationCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(Quaternion);
    }
}
