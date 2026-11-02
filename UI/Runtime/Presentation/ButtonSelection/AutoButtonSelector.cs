using System.Threading;
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

        [Tooltip("Optional. Selected when this panel closes and the previous selection can't be restored " +
                 "(for example, its menu was deactivated while this panel was open). " +
                 "Leave empty to select nothing in that case.")]
        [SerializeField] private Button _fallbackButton;

        private GameObject _lastSelected;

        private void OnEnable()
        {
            EventSystem eventSystem = EventSystem.current;

            // Always overwrite, even with null, so a selection from an earlier opening is never restored by mistake.
            _lastSelected = eventSystem ? eventSystem.currentSelectedGameObject : null;

            // Selecting the button in OnEnable doesn't register OnSelect by ISelectHandler,
            // so we delay it to the next frame.
            DelayedSelect(_defaultButton, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid DelayedSelect(Button button, CancellationToken token)
        {
            await UniTask.NextFrame(token);

            // The panel may have been disabled during the wait, or the EventSystem may be gone.
            if (!isActiveAndEnabled || !EventSystem.current || !button)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }

        private void OnDisable()
        {
            // EventSystem can be null during scene teardown or application quit.
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem)
            {
                return;
            }

            if (SelectionUtility.IsValidTarget(_lastSelected))
            {
                eventSystem.SetSelectedGameObject(_lastSelected);
                return;
            }

            if (_fallbackButton && SelectionUtility.IsValidTarget(_fallbackButton.gameObject))
            {
                eventSystem.SetSelectedGameObject(_fallbackButton.gameObject);
            }
        }
    }
}
