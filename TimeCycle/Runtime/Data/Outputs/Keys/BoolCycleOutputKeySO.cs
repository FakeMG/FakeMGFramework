using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TIME_CYCLE + "/Output Keys/Bool")]
    public sealed class BoolCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(bool);
    }
}
