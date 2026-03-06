namespace FakeMG.Framework.ExtensionMethods
{
    public static class NumericExtensions
    {
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }

        public static int Remap(this int value, int fromMin, int fromMax, int toMin, int toMax)
        {
            // We cast to float internally to maintain precision during the division
            return (int)Remap((float)value, fromMin, fromMax, toMin, toMax);
        }
    }
}