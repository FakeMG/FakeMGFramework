using System;
using UnityEngine;

namespace FakeMG.Framework.Drag
{
    public class Draggable : MonoBehaviour
    {
        public bool IsDragging { get; private set; }

        public event Action<Draggable> OnDragStarted;
        public event Action<Draggable> OnDragEnded;

        private object _dropClaimer;

        public void StartDrag()
        {
            IsDragging = true;
            _dropClaimer = null;
            OnDragStarted?.Invoke(this);
        }

        public void EndDrag()
        {
            IsDragging = false;
            OnDragEnded?.Invoke(this);
            _dropClaimer = null;
        }

        /// <summary>
        /// Attempts to claim the drop for a specific handler.
        /// Only the first claimer succeeds, ensuring only one DropHandler reacts.
        /// </summary>
        public bool TryClaimDrop(object claimer)
        {
            if (_dropClaimer != null) return false;

            _dropClaimer = claimer;
            return true;
        }
    }
}
