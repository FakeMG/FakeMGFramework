using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeMG.Settings
{
    public sealed class SelectOnPointerEnter : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private Selectable _selectable;

        #region Public Methods

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selectable.IsInteractable())
            {
                _selectable.Select();
            }
        }

        #endregion
    }
}
