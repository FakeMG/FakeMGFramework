using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.FakeMGFramework.UI.Popup
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupAnimator : MonoBehaviour
    {
        [Required]
        [SerializeField] private PopupManager popupManager;

        [Header("Events")]
        [SerializeField] public UnityEvent onShowStart;
        [SerializeField] public UnityEvent onShowFinished;
        [SerializeField] public UnityEvent onHideStart;
        [SerializeField] public UnityEvent onHideFinished;
        
        protected Sequence CurrentSequence;

        public abstract void Show(bool animate = true);
        public abstract void Hide(bool animate = true);
    }
}