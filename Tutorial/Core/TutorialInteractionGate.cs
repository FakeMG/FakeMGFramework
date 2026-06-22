using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Central allow-list of targets. With no restriction every interaction is allowed;
    /// with an allow-list only the listed targets are. Gameplay input is never globally
    /// paused here, only filtered: <see cref="TutorialRaycasterFilter"/> asks the gate
    /// whether a UI hit is allowed, so interactables need no tutorial knowledge.
    /// </summary>
    public sealed class TutorialInteractionGate
    {
        private readonly List<ITutorialTarget> _allowedTargets = new();

        public bool HasRestriction { get; private set; }

        public void RestrictToAllowList(IReadOnlyList<ITutorialTarget> allowedTargets)
        {
            _allowedTargets.Clear();
            for (int i = 0; i < allowedTargets.Count; i++)
            {
                _allowedTargets.Add(allowedTargets[i]);
            }

            HasRestriction = true;
        }

        public void ClearRestrictions()
        {
            _allowedTargets.Clear();
            HasRestriction = false;
        }

        /// <summary>
        /// True when a raycast hit belongs to an allowed UI target (the hit object is the
        /// target or nested under it). Used by the raycaster filter to drop other hits.
        /// </summary>
        public bool IsRaycastAllowed(GameObject hitObject)
        {
            if (!HasRestriction) return true;

            Transform hit = hitObject.transform;
            for (int targetIndex = _allowedTargets.Count - 1; targetIndex >= 0; targetIndex--)
            {
                ITutorialTarget target = _allowedTargets[targetIndex];
                if (target.IsDestroyed())
                {
                    _allowedTargets.RemoveAt(targetIndex);
                    continue;
                }

                Transform allowed = target.InteractionTransform;
                if (allowed != null && (hit == allowed || hit.IsChildOf(allowed)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
