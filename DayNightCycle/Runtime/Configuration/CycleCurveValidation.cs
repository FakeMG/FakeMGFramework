using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Validates every numeric field used by authored animation curves.
    /// </summary>
    internal static class CycleCurveValidation
    {
        #region Public Methods

        public static bool TryValidate(AnimationCurve curve, out string errorMessage)
        {
            if (curve == null)
            {
                errorMessage = "An animation curve is required.";
                return false;
            }

            Keyframe[] keys = curve.keys;
            for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
            {
                Keyframe key = keys[keyIndex];
                if (!CycleNumericValidation.IsFinite(key.time)
                    || !CycleNumericValidation.IsFinite(key.value)
                    || !CycleNumericValidation.IsFinite(key.inTangent)
                    || !CycleNumericValidation.IsFinite(key.outTangent)
                    || !CycleNumericValidation.IsFinite(key.inWeight)
                    || !CycleNumericValidation.IsFinite(key.outWeight))
                {
                    errorMessage = $"Animation curve key {keyIndex} contains a non-finite value.";
                    return false;
                }
            }

            errorMessage = null;
            return true;
        }

        #endregion
    }
}
