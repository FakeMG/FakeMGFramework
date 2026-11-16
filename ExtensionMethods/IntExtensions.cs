namespace FakeMG.Framework.ExtensionMethods
{
    public static class IntExtensions
    {
        public static int WrapValue(this int value, int minIncluded, int maxIncluded)
        {
            if (minIncluded == maxIncluded)
            {
                return minIncluded;
            }

            int range = maxIncluded - minIncluded + 1;

            return ((value - minIncluded) % range + range) % range + minIncluded;
        }
    }
}