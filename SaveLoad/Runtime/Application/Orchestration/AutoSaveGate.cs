using System;
using System.Collections.Generic;

namespace FakeMG.SaveLoad
{
    /// <summary>Shared by the interval and lifecycle autosaves, so one suspension silences both.</summary>
    public sealed class AutoSaveGate : IAutoSaveGate
    {
        private readonly HashSet<Suspension> _suspensions = new();

        public bool IsSuspended => _suspensions.Count > 0;

        #region Public Methods

        public IDisposable SuspendAutoSave(string reason)
        {
            var suspension = new Suspension(this, reason);
            _suspensions.Add(suspension);
            return suspension;
        }

        #endregion

        private sealed class Suspension : IDisposable
        {
            private readonly AutoSaveGate _owner;
            private bool _hasReleased;
            public string Reason { get; }

            #region Public Methods

            public Suspension(AutoSaveGate owner, string reason)
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
