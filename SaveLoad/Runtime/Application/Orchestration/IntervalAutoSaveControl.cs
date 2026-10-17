using System;
using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    public interface IIntervalAutoSaveControl
    {
        IDisposable SuspendIntervalAutoSave(string reason);
    }

    internal sealed class IntervalAutoSaveGate
    {
        private readonly HashSet<Suspension> _suspensions = new();
        public bool IsSuspended => _suspensions.Count > 0;

        #region Public Methods

        public IDisposable SuspendIntervalAutoSave(string reason)
        {
            var suspension = new Suspension(this, reason);
            _suspensions.Add(suspension);
            return suspension;
        }

        #endregion

        private sealed class Suspension : IDisposable
        {
            private readonly IntervalAutoSaveGate _owner;
            private bool _hasReleased;
            public string Reason { get; }

            #region Public Methods

            public Suspension(IntervalAutoSaveGate owner, string reason)
            {
                _owner = owner;
                Reason = reason;
            }

            public void Dispose()
            {
                if (_hasReleased)
                {
                    return;
                }

                _hasReleased = true;
                _owner._suspensions.Remove(this);
            }

            #endregion
        }
    }
}
