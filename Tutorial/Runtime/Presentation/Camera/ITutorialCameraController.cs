using System.Threading;
using Cysharp.Threading.Tasks;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Game-agnostic camera control for tutorial steps. Games provide a concrete
    /// implementation (e.g. Cinemachine); the framework ships a no-op default.
    /// Movement and restoration must animate smoothly, never snap.
    /// </summary>
    public interface ITutorialCameraController
    {
        UniTask MoveToAsync(TutorialCameraSetting setting, CancellationToken cancellationToken);
        UniTask RestoreAsync(CancellationToken cancellationToken);
    }
}
