using System;

namespace FakeMG.Tutorial
{
    public interface ITutorialActivatableTarget : ITutorialTarget
    {
        event Action OnActivated;
    }
}
