using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TIME_CYCLE + "/Output Keys/Int")]
    public sealed class IntCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(int);
    }
}
