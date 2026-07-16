using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Owns the lifetime, monotonic progress, cancellation, and completion of one active time command.
    /// </summary>
    internal sealed class TimeCommandRunner
    {
        private ActiveCommand _activeCommand;
        private long _nextCommandId;

        public bool IsActive => _activeCommand != null;
        public bool IsCancellationRequested => _activeCommand?.CancellationToken.IsCancellationRequested == true;
        public TimeCommandMode Mode => _activeCommand.Mode;
        public TimeMovementDirection Direction => _activeCommand.Direction;
        public double TargetTimeSeconds => _activeCommand.TargetTimeSeconds;
        public double StartTimeSeconds => _activeCommand.StartTimeSeconds;
        public double DirectedDistanceSeconds => _activeCommand.DirectedDistanceSeconds;

        #region Public Methods

        public long CreateCommandId()
        {
            return ++_nextCommandId;
        }

        public UniTask<TimeCommandResult> Start(
            long commandId,
            TimeCommandMode mode,
            TimeMovementDirection direction,
            double targetTimeSeconds,
            double startTimeSeconds,
            double directedDistanceSeconds,
            float durationSeconds,
            AnimationCurve transitionCurve,
            CancellationToken cancellationToken)
        {
            _activeCommand = new ActiveCommand(
                commandId,
                mode,
                direction,
                targetTimeSeconds,
                startTimeSeconds,
                directedDistanceSeconds,
                durationSeconds,
                transitionCurve,
                cancellationToken);
            return _activeCommand.CompletionSource.Task;
        }

        public TimeCommandFrame Tick(float deltaTimeSeconds)
        {
            _activeCommand.ElapsedSeconds += deltaTimeSeconds;
            float linearProgress01 = Math.Clamp(
                _activeCommand.ElapsedSeconds / _activeCommand.DurationSeconds,
                0f,
                1f);
            float evaluatedProgress01 = Math.Clamp(
                _activeCommand.TransitionCurve.Evaluate(linearProgress01),
                0f,
                1f);
            float directedProgress01 = Math.Max(
                _activeCommand.DirectedProgress01,
                evaluatedProgress01);
            if (linearProgress01 >= 1f)
            {
                directedProgress01 = 1f;
            }

            double distanceDeltaSeconds = _activeCommand.DirectedDistanceSeconds
                                          * (directedProgress01 - _activeCommand.DirectedProgress01);
            _activeCommand.DirectedProgress01 = directedProgress01;
            return new TimeCommandFrame(
                distanceDeltaSeconds * (int)_activeCommand.Direction,
                directedProgress01,
                linearProgress01 >= 1f);
        }

        public bool TryComplete(TimeCommandStatus status, out TimeCommandResult result)
        {
            if (_activeCommand == null)
            {
                result = default;
                return false;
            }

            ActiveCommand completedCommand = _activeCommand;
            _activeCommand = null;
            result = new TimeCommandResult(completedCommand.CommandId, status);
            completedCommand.CompletionSource.TrySetResult(result);
            return true;
        }

        #endregion

        /// <summary>
        /// Stores mutable state belonging only to the active command execution.
        /// </summary>
        private sealed class ActiveCommand
        {
            public long CommandId { get; }
            public TimeCommandMode Mode { get; }
            public TimeMovementDirection Direction { get; }
            public double TargetTimeSeconds { get; }
            public double StartTimeSeconds { get; }
            public double DirectedDistanceSeconds { get; }
            public float DurationSeconds { get; }
            public AnimationCurve TransitionCurve { get; }
            public CancellationToken CancellationToken { get; }
            public UniTaskCompletionSource<TimeCommandResult> CompletionSource { get; } = new();
            public float ElapsedSeconds { get; set; }
            public float DirectedProgress01 { get; set; }

            public ActiveCommand(
                long commandId,
                TimeCommandMode mode,
                TimeMovementDirection direction,
                double targetTimeSeconds,
                double startTimeSeconds,
                double directedDistanceSeconds,
                float durationSeconds,
                AnimationCurve transitionCurve,
                CancellationToken cancellationToken)
            {
                CommandId = commandId;
                Mode = mode;
                Direction = direction;
                TargetTimeSeconds = targetTimeSeconds;
                StartTimeSeconds = startTimeSeconds;
                DirectedDistanceSeconds = directedDistanceSeconds;
                DurationSeconds = durationSeconds;
                TransitionCurve = transitionCurve;
                CancellationToken = cancellationToken;
            }
        }
    }
}
