using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TIME_CYCLE + "/Output Keys/Rotation")]
    public sealed class RotationCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(Quaternion);
    }
}
