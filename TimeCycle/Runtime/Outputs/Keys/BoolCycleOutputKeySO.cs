using System;
using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Identifies one boolean cycle output.
    /// </summary>
    [CreateAssetMenu(menuName = FakeMGEditorMenus.TIME_CYCLE + "/Output Keys/Bool")]
    public sealed class BoolCycleOutputKeySO : CycleOutputKeySO
    {
        public override Type ValueType => typeof(bool);
    }
}
