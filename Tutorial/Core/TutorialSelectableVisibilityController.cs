using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Hides every interactable UI widget (any <see cref="Selectable"/> — buttons, toggles,
    /// sliders, ...) on a HUD canvas except those inside the allowed roots, scaling them out
    /// and disabling interaction, and restores their prior scale and interactable state with
    /// a smooth animation when the step ends.
    /// </summary>
    public sealed class TutorialSelectableVisibilityController
    {
        private const float RESTORE_DURATION_SECONDS = 0.35f;

        private readonly Dictionary<GameObject, HiddenInteractableState> _hiddenInteractables = new();

        #region Public Methods

        // TODO: make this hide non-interactable widgets as well
        public void ApplyVisibility(
            RectTransform canvasRoot,
            IReadOnlyList<Transform> visibleRoots,
            IReadOnlyList<RectTransform> alwaysVisibleRoots)
        {
            Selectable[] interactables = canvasRoot.GetComponentsInChildren<Selectable>(true);
            for (int interactableIndex = 0; interactableIndex < interactables.Length; interactableIndex++)
            {
                GameObject interactableObject = interactables[interactableIndex].gameObject;
                bool isVisible = IsInsideAnyRoot(interactableObject.transform, visibleRoots) ||
                                 IsInsideAnyRoot(interactableObject.transform, alwaysVisibleRoots);

                if (isVisible)
                {
                    RestoreInteractable(interactableObject);
                    continue;
                }

                HideInteractable(interactableObject);
            }
        }

        public async UniTask RestoreAllAsync(CancellationToken cancellationToken)
        {
            var restoreAnimations = new List<UniTask>(_hiddenInteractables.Count);
            foreach (KeyValuePair<GameObject, HiddenInteractableState> hiddenInteractable in _hiddenInteractables)
            {
                if (hiddenInteractable.Key == null)
                {
                    continue;
                }

                restoreAnimations.Add(AnimateRestoreAsync(
                    hiddenInteractable.Key,
                    hiddenInteractable.Value,
                    cancellationToken));
            }

            _hiddenInteractables.Clear();
            await UniTask.WhenAll(restoreAnimations);
        }

        #endregion

        #region Private Methods

        private void HideInteractable(GameObject interactableObject)
        {
            if (_hiddenInteractables.ContainsKey(interactableObject) || !interactableObject.activeSelf)
            {
                return;
            }

            Selectable selectable = interactableObject.GetComponent<Selectable>();
            _hiddenInteractables.Add(interactableObject, new HiddenInteractableState(
                interactableObject.transform.localScale,
                selectable.interactable));
            selectable.interactable = false;
            interactableObject.transform.localScale = Vector3.zero;
        }

        private void RestoreInteractable(GameObject interactableObject)
        {
            if (!_hiddenInteractables.Remove(interactableObject, out HiddenInteractableState hiddenInteractableState))
            {
                return;
            }

            AnimateRestoreAsync(interactableObject, hiddenInteractableState, CancellationToken.None).Forget();
        }

        private static async UniTask AnimateRestoreAsync(
            GameObject interactableObject,
            HiddenInteractableState hiddenInteractableState,
            CancellationToken cancellationToken)
        {
            Transform interactableTransform = interactableObject.transform;
            interactableTransform.localScale = Vector3.zero;
            await interactableTransform.DOScale(hiddenInteractableState.OriginalScale, RESTORE_DURATION_SECONDS)
                .SetEase(Ease.OutBack)
                .ToUniTask(cancellationToken: cancellationToken);
            interactableObject.GetComponent<Selectable>().interactable = hiddenInteractableState.WasInteractable;
        }

        private static bool IsInsideAnyRoot<T>(Transform target, IReadOnlyList<T> roots) where T : Transform
        {
            for (int rootIndex = 0; rootIndex < roots.Count; rootIndex++)
            {
                Transform root = roots[rootIndex];
                if (root != null && (target == root || target.IsChildOf(root)))
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct HiddenInteractableState
        {
            public HiddenInteractableState(Vector3 originalScale, bool wasInteractable)
            {
                OriginalScale = originalScale;
                WasInteractable = wasInteractable;
            }

            public Vector3 OriginalScale { get; }
            public bool WasInteractable { get; }
        }

        #endregion
    }
}
