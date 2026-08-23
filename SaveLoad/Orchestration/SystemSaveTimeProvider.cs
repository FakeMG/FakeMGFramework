using System;

namespace FakeMG.SaveLoad
{
    public sealed class SystemSaveTimeProvider : ISaveTimeProvider
    {
        public DateTime GetUtcNow()
        {
            return DateTime.UtcNow;
        }
    }
}
