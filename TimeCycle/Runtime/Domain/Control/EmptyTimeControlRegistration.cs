using System;

namespace FakeMG.TimeCycle
{
    /// <summary>
    /// Provides a safe immutable registration when a control request is rejected.
    /// </summary>
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
