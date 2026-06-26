using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using VContainer;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// GraphicRaycaster that drops UI hits outside the tutorial's allowed targets while
    /// a restriction is active. Replaces the default GraphicRaycaster on a canvas so the
    /// EventSystem simply never reports blocked elements; interactables stay unaware of
    /// the tutorial. Fires <see cref="OnBlockedSelectableClicked"/> when the player
    /// presses a Selectable that was dropped by the filter.
    /// </summary>
    public sealed class TutorialRaycasterFilter : GraphicRaycaster
    {
        [SerializeField] private List<RectTransform> _alwaysAllowedRoots = new();

        private TutorialInteractionGate _gate;
        private int _lastBlockedNotifyFrameCount = -1;

        public event Action OnBlockedSelectableClicked;

        [Inject]
        public void Construct(TutorialInteractionGate gate)
        {
            _gate = gate;
        }

        public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
        {
            base.Raycast(eventData, resultAppendList);

            if (_gate == null || !_gate.HasRestriction) return;

            bool hadBlockedSelectable = false;
            for (int i = resultAppendList.Count - 1; i >= 0; i--)
            {
                GameObject hitObject = resultAppendList[i].gameObject;
                if (!IsAlwaysAllowed(hitObject.transform) && !_gate.IsRaycastAllowed(hitObject))
                {
                    if (!hadBlockedSelectable && hitObject.GetComponentInParent<Selectable>() != null)
                        hadBlockedSelectable = true;
                    resultAppendList.RemoveAt(i);
                }
            }

            if (hadBlockedSelectable && Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
                NotifyBlockedSelectableClicked();
        }

        private void NotifyBlockedSelectableClicked()
        {
            if (Time.frameCount == _lastBlockedNotifyFrameCount) return;
            _lastBlockedNotifyFrameCount = Time.frameCount;
            OnBlockedSelectableClicked?.Invoke();
        }

        private bool IsAlwaysAllowed(Transform hitTransform)
        {
            for (int rootIndex = 0; rootIndex < _alwaysAllowedRoots.Count; rootIndex++)
            {
                RectTransform root = _alwaysAllowedRoots[rootIndex];
                if (root != null && (hitTransform == root || hitTransform.IsChildOf(root)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
