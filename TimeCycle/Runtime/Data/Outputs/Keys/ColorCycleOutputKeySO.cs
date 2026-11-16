using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TIME_CYCLE + "/Output Keys/Color")]
    public sealed class ColorCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(Color);
    }
}
