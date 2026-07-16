using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.DayNightCycle
{
    /// <summary>
    /// Defines the complete public control surface of a time-of-cycle runtime.
    /// </summary>
    public interface ITimeOfCycle
    {
        TimeOfCycleState CurrentState { get; }

        event Action<CyclePeriodChange> OnPeriodChanged;
        event Action OnCycleCompleted;
        event Action<TimeCommandResult> OnTimeCommandCompleted;

        void SetAutomaticAdvancementEnabled(bool isEnabled);
        bool TrySetAdvancementRate(double advancementRateCycleSecondsPerRealSecond);
        UniTask<TimeCommandResult> ExecuteTimeCommandAsync(
            TimeCommand command,
            CancellationToken cancellationToken = default);
        IDisposable RegisterControl(TimeControlRequest request);
        bool SetContextOverride(TimeOfCycleOverrideSO contextOverrideSO);
        bool SetRuntimeOverride(TimeOfCycleOverrideSO runtimeOverrideSO);
    }
}
