using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Toast
{
    /// <summary>
    /// Replaceable animation component for a toast view. Swap the concrete component
    /// on the toast prefab to change animation style without touching the core toast logic.
    /// Implementations must use unscaled time so toasts keep animating while the game is paused.
    /// </summary>
    public abstract class ToastAnimator : MonoBehaviour
    {
        public abstract UniTask ShowAsync(CancellationToken cancellationToken);
        public abstract UniTask HideAsync(CancellationToken cancellationToken);
        public abstract UniTask MoveToAsync(Vector2 anchoredPositionPixels, CancellationToken cancellationToken);

        /// <summary>Kills running tweens and resets visuals so the view can be reused from the pool.</summary>
        public abstract void SetVisualsToHiddenState();
    }
}
