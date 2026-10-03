using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.TimeCycle
{
    public interface IWorldTimeCommands
    {
        UniTask<TimeCommandResult> JumpToDayAsync(long dayNumber, double cycleProgress01, CancellationToken cancellationToken = default);
    }
}
