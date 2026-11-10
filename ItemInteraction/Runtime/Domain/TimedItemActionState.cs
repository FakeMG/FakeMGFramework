using System;

namespace FakeMG.ItemInteraction
{
    public readonly struct TimedItemActionOutcome
    {
        public bool ShouldAttemptCommit { get; }
        public bool HasCompleted { get; }
        public bool HasCanceled { get; }

        public TimedItemActionOutcome(bool shouldAttemptCommit, bool hasCompleted, bool hasCanceled)
        {
            ShouldAttemptCommit = shouldAttemptCommit;
            HasCompleted = hasCompleted;
            HasCanceled = hasCanceled;
        }
    }

    public sealed class TimedItemActionState
    {
        private readonly IItemInputLifecycle _inputLifecycle;
        private readonly float _actionMomentSeconds;
        private readonly float _completionSeconds;

        private float _elapsedSeconds;
        private bool _isInputHeld = true;
        private bool _hasRequestedCommit;

        public bool IsActive { get; private set; } = true;

        public TimedItemActionState(IItemInputLifecycle inputLifecycle, float actionMomentSeconds,
            float completionSeconds)
        {
            _inputLifecycle = inputLifecycle ?? throw new ArgumentNullException(nameof(inputLifecycle));
            _actionMomentSeconds = Math.Max(0f, actionMomentSeconds);
            _completionSeconds = Math.Max(_actionMomentSeconds, completionSeconds);
        }

        #region Public Methods

        public TimedItemActionOutcome Tick(float deltaSeconds)
        {
            if (!IsActive || deltaSeconds < 0f)
            {
                return default;
            }

            _elapsedSeconds += deltaSeconds;
            bool shouldAttemptCommit = !_hasRequestedCommit && _elapsedSeconds >= _actionMomentSeconds &&
                                       _inputLifecycle.CanCommitAtActionMoment(_isInputHeld);
            if (shouldAttemptCommit)
            {
                _hasRequestedCommit = true;
            }

            bool hasCompleted = _elapsedSeconds >= _completionSeconds;
            if (hasCompleted)
            {
                IsActive = false;
            }

            return new TimedItemActionOutcome(shouldAttemptCommit, hasCompleted, false);
        }

        public TimedItemActionOutcome Release()
        {
            if (!IsActive)
            {
                return default;
            }

            _isInputHeld = false;
            bool hasCanceled = !_hasRequestedCommit && _elapsedSeconds < _actionMomentSeconds &&
                               _inputLifecycle.ShouldCancelWhenReleasedBeforeActionMoment;
            if (hasCanceled)
            {
                IsActive = false;
            }

            return new TimedItemActionOutcome(false, false, hasCanceled);
        }

        public TimedItemActionOutcome Cancel()
        {
            if (!IsActive)
            {
                return default;
            }

            IsActive = false;
            _isInputHeld = false;
            return new TimedItemActionOutcome(false, false, true);
        }

        #endregion
    }
}
