namespace FakeMG.TimeCycle
{
    internal static class CycleProgressConversion
    {
        #region Public Methods

        public static double ResolveTimeSeconds(double progress01, double cycleDurationSeconds)
        {
            return progress01 * cycleDurationSeconds;
        }

        public static bool IsValid(double progress01)
        {
            return CycleNumericValidation.IsFinite(progress01) && progress01 >= 0d && progress01 < 1d;
        }

        #endregion
    }
}
