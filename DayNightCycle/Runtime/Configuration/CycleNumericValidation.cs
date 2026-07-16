namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Centralizes finite-number invariants shared by cycle configuration and runtime commands.
    /// </summary>
    internal static class CycleNumericValidation
    {
        #region Public Methods

        public static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        public static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        public static bool IsFiniteNonNegative(double value)
        {
            return IsFinite(value) && value >= 0d;
        }

        public static bool IsFiniteNonNegative(float value)
        {
            return IsFinite(value) && value >= 0f;
        }

        #endregion
    }
}
