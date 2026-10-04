using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Component on an addressable visual prefab that knows how to animate itself in and
    /// out. Visual modules drive views through this common show/hide interface.
    /// </summary>
    public abstract class TutorialVisualView : MonoBehaviour
    {
        public abstract UniTask ShowAsync(CancellationToken cancellationToken);
        public abstract UniTask HideAsync(CancellationToken cancellationToken);
    }
}
