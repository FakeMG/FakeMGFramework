using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TIME_CYCLE + "/Output Keys/Float")]
    public sealed class FloatCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(float);
    }
}
