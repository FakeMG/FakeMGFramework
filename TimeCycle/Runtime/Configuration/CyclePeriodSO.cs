using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.TimeCycle
{
    [CreateAssetMenu(menuName = "FakeMG/Time Cycle/Period SO")]
    public sealed class CyclePeriodSO : IdentitySO
    {
        public CyclePeriodId PeriodId => new(Id);

        #region Public Methods

#if UNITY_EDITOR
        public void ConfigureForEditor(string periodId, string displayName)
        {
            ConfigureIdentityForEditor(periodId, displayName, null);
        }
#endif

        #endregion
    }
}
