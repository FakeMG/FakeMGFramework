using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.FakeMGFramework.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class PopupAnimator : MonoBehaviour
    {
        [Required]
        [SerializeField] private PopupManager popupManager;

        [Header("Root Canvas")]
        [SerializeField] protected RectTransform canvasRect;

        [Header("Events")]
        [SerializeField] public UnityEvent onShowStart;
        [SerializeField] public UnityEvent onShowFinished;
        [SerializeField] public UnityEvent onHideStart;
        [SerializeField] public UnityEvent onHideFinished;

        public abstract void Show(bool animate = true);
        public abstract void Hide(bool animate = true);
    }
}