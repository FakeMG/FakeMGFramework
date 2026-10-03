using System;
using UnityEditor;
using UnityEngine;

namespace FakeMG.TimeCycle.Editor
{
    /// <summary>Preserves shared period asset identity when display labels or filenames change.</summary>
    public static class CyclePeriodAssetAuthoring
    {
        #region Public Methods

        public static CyclePeriodSO CreateOrUpdatePeriodSO(string assetFolderPath, string periodId, string displayName)
        {
            CyclePeriodSO periodSO = null;
            var expectedId = new CyclePeriodId(periodId);
            foreach (string guid in AssetDatabase.FindAssets("t:CyclePeriodSO", new[] { assetFolderPath }))
            {
                var candidateSO = AssetDatabase.LoadAssetAtPath<CyclePeriodSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (!candidateSO.PeriodId.Equals(expectedId)) continue;
                if (periodSO != null)
                {
                    throw new InvalidOperationException($"Period ID '{periodId}' belongs to multiple assets. Remove the duplicate first.");
                }
                periodSO = candidateSO;
            }
            if (periodSO == null)
            {
                periodSO = ScriptableObject.CreateInstance<CyclePeriodSO>();
                AssetDatabase.CreateAsset(periodSO, assetFolderPath + "/" + periodId + ".asset");
            }
            periodSO.ConfigureForEditor(periodId, displayName);
            EditorUtility.SetDirty(periodSO);
            return periodSO;
        }

        #endregion
    }
}
