using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.Framework
{
    public enum StartupReadinessStatus
    {
        Ready,
        Failed,
        Cancelled
    }

    public readonly struct StartupReadinessResult
    {
        public StartupReadinessStatus Status { get; }

        public string FailureReason { get; }

        public bool Succeeded => Status == StartupReadinessStatus.Ready;

        private StartupReadinessResult(StartupReadinessStatus status, string failureReason)
        {
            Status = status;
            FailureReason = failureReason ?? string.Empty;
        }

        public static StartupReadinessResult Ready()
        {
            return new StartupReadinessResult(StartupReadinessStatus.Ready, string.Empty);
        }

        public static StartupReadinessResult Failed(string failureReason)
        {
            return new StartupReadinessResult(StartupReadinessStatus.Failed, failureReason);
        }

        public static StartupReadinessResult Cancelled(string failureReason)
        {
            return new StartupReadinessResult(StartupReadinessStatus.Cancelled, failureReason);
        }
    }

    public interface IStartupReadiness
    {
        UniTask<StartupReadinessResult> WaitUntilReadyAsync(CancellationToken cancellationToken);
    }
}
