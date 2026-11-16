using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Tutorial
{
    public abstract class TutorialVisualView : MonoBehaviour
    {
        public abstract UniTask ShowAsync(CancellationToken cancellationToken);
        public abstract UniTask HideAsync(CancellationToken cancellationToken);
    }
}
