namespace FakeMG.FakeMGFramework.ExtensionMethods
{
    public static class IntExtensions
    {
        public static int WrapValue(this int value, int minIncluded, int maxIncluded)
        {
            // Handle the case where min equals max
            if (minIncluded == maxIncluded)
            {
                return minIncluded;
            }

            // Calculate range
            int range = maxIncluded - minIncluded + 1;

            // Normal case - wrap the value around the range
            return ((value - minIncluded) % range + range) % range + minIncluded;
        }
    }
}