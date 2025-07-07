using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FakeMG.FakeMGFramework.UI.Tab.TabButton
{
    //TODO: doesn't support navigation keys, only mouse and touch input
    public abstract class TabButtonBase : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler,
        IPointerExitHandler
    {
        public UnityEvent onClick;
        
        public abstract void AnimateSelection();
        public abstract void AnimateDeselection();
        public abstract void InstantlySelect();
        public abstract void InstantlyDeselect();

        public void OnPointerDown(PointerEventData eventData)
        {
        }
        
        public void OnPointerUp(PointerEventData eventData)
        {
            //prevent multi touch
            if (Input.touchCount > 1) return;
            
            onClick?.Invoke();
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
        }
    }
}