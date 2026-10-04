using System;
using Cysharp.Threading.Tasks;

namespace FakeMG.SceneTransition
{
    public interface ISceneTransition
    {
        UniTask PlayTransitionAsync(Action process);

        UniTask PlayTransitionAsync(Func<UniTask> processTask);
    }
}
