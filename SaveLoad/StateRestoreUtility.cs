namespace FakeMG.SaveLoad
{
    public static class StateRestoreUtility
    {
        public static bool TryRestore<TState>(object data, out TState state)
            where TState : class
        {
            if (data is TState restoredState)
            {
                state = restoredState;
                return true;
            }

            state = null;
            return false;
        }
    }
}
