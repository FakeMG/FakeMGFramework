using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.ButtonSelection
{
    /// <summary>
    /// Automatically selects a default button when enabled and restores the previous selection when disabled.
    /// <para>
    /// Used for pop-up menus or panels to ensure a button is always selected.
    /// </para>
    /// </summary>
    public class AutoButtonSelector : MonoBehaviour
    {
        [SerializeField] private Button _defaultButton;

        private GameObject _lastSelected;

        private void OnEnable()
        {
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                _lastSelected = EventSystem.current.currentSelectedGameObject;
            }
            // Select the button in OnEnable doesn't register OnSelect by ISelectHandler,
            // so we delay it to the next frame.
            DelayedSelect(_defaultButton).Forget();
        }

        private async UniTaskVoid DelayedSelect(Button button)
        {
            await UniTask.NextFrame();
            if (gameObject.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        private void OnDisable()
        {
            if (_lastSelected != null && _lastSelected.activeInHierarchy)
            {
                EventSystem.current.SetSelectedGameObject(_lastSelected);
            }
        }
    }
}