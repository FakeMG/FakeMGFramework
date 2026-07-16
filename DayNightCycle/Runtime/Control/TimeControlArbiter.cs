using System;
using System.Collections.Generic;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Owns persistent-control registrations and selects the newest highest-priority valid request.
    /// </summary>
    internal sealed class TimeControlArbiter
    {
        private readonly List<Registration> _registrations = new();
        private readonly Action _notifyActiveControlChanged;

        private Registration _activeRegistration;
        private long _nextSequence;

        public TimeControlRequest ActiveRequest => _activeRegistration?.Request;

        public TimeControlArbiter(Action notifyActiveControlChanged)
        {
            _notifyActiveControlChanged = notifyActiveControlChanged;
        }

        #region Public Methods

        public IDisposable Register(TimeControlRequest request)
        {
            Registration registration = new(this, request, ++_nextSequence);
            _registrations.Add(registration);
            Arbitrate();
            return registration;
        }

        public void Clear()
        {
            _registrations.Clear();
            _activeRegistration = null;
        }

        public bool RemoveInvalidRegistrations()
        {
            bool hasRemovedRegistration = false;
            for (int registrationIndex = _registrations.Count - 1;
                 registrationIndex >= 0;
                 registrationIndex--)
            {
                Registration registration = _registrations[registrationIndex];
                if (registration.IsReleased || !registration.Request.Strategy.IsValid)
                {
                    registration.MarkReleased();
                    _registrations.RemoveAt(registrationIndex);
                    hasRemovedRegistration = true;
                }
            }

            Arbitrate();
            return hasRemovedRegistration;
        }

        public void ReleaseActiveRequest()
        {
            if (_activeRegistration != null)
            {
                Release(_activeRegistration);
            }
        }

        #endregion

        #region Private Methods

        private void Release(Registration registration)
        {
            if (registration.IsReleased)
            {
                return;
            }

            registration.MarkReleased();
            _registrations.Remove(registration);
            Arbitrate();
        }

        private void Arbitrate()
        {
            Registration selectedRegistration = null;
            for (int registrationIndex = 0;
                 registrationIndex < _registrations.Count;
                 registrationIndex++)
            {
                Registration candidate = _registrations[registrationIndex];
                if (selectedRegistration == null
                    || candidate.Request.Priority > selectedRegistration.Request.Priority
                    || candidate.Request.Priority == selectedRegistration.Request.Priority
                    && candidate.Sequence > selectedRegistration.Sequence)
                {
                    selectedRegistration = candidate;
                }
            }

            if (ReferenceEquals(_activeRegistration, selectedRegistration))
            {
                return;
            }

            _activeRegistration = selectedRegistration;
            _notifyActiveControlChanged();
        }

        #endregion

        /// <summary>
        /// Owns the disposable lifetime of one persistent-control request.
        /// </summary>
        private sealed class Registration : IDisposable
        {
            private readonly TimeControlArbiter _owner;

            public TimeControlRequest Request { get; }
            public long Sequence { get; }
            public bool IsReleased { get; private set; }

            public Registration(TimeControlArbiter owner, TimeControlRequest request, long sequence)
            {
                _owner = owner;
                Request = request;
                Sequence = sequence;
            }

            #region Public Methods

            public void Dispose()
            {
                _owner.Release(this);
            }

            public void MarkReleased()
            {
                IsReleased = true;
            }

            #endregion
        }
    }
}
