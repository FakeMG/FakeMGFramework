using System;

namespace FakeMG.TimeCycle
{
    internal sealed class EmptyTimeControlRegistration : IDisposable
    {
        public static EmptyTimeControlRegistration Instance { get; } = new();

        #region Public Methods

        public void Dispose()
        {
        }

        #endregion
    }
}
