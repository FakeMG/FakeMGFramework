using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace FakeMG.Framework.UI.ButtonSelection
{
    /// <summary>
    /// Restores a selection when the player uses the controller/keyboard after the selection was lost.
    /// <para>
    /// Selection is lost when the player clicks empty space with the mouse (the EventSystem clears it),
    /// or when the selected object gets deactivated.
    /// </para>
    /// <para>
    /// Add one instance to a persistent object (for example the EventSystem object).
    /// The first navigation input only brings the highlight back; it is not also treated as a move or a click.
    /// </para>
    /// </summary>
    // Runs after the EventSystem (order 0) so the input that restores the selection
    // isn't processed by the input module as a move/submit on the newly selected object in the same frame.
    [DefaultExecutionOrder(100)]
    public class SelectionRecovery : MonoBehaviour
    {
        [Tooltip("Optional. Selected when there is no valid previous selection to go back to.")]
        [SerializeField] private Selectable _fallbackSelectable;

#if ENABLE_INPUT_SYSTEM
        [Tooltip("Navigate and Submit actions. Use the same actions asset as the Input System UI Input Module.")]
        [SerializeField] private InputActionReference[] _navigationActions;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        [SerializeField] private string _horizontalAxis = "Horizontal";
        [SerializeField] private string _verticalAxis = "Vertical";
        [SerializeField] private string _submitButton = "Submit";
        [SerializeField] private float _axisThreshold = 0.5f;
#endif

        private GameObject _lastValidSelected;

        private void Update()
        {
            EventSystem eventSystem = EventSystem.current;
            if (!eventSystem) return;

            GameObject current = eventSystem.currentSelectedGameObject;

            if (SelectionUtility.IsAlive(current))
            {
                // Only remember objects that are safe to go back to.
                // A selected but non-interactable button is left alone so normal navigation off it still works.
                if (SelectionUtility.IsValidTarget(current))
                {
                    _lastValidSelected = current;
                }

                return;
            }

            // Selection is lost (null or deactivated). Wait for navigation input before bringing it back,
            // so mouse users don't get a highlight they didn't ask for.
            if (!WasNavigationInputThisFrame()) return;

            GameObject target = ResolveTarget();
            if (target)
            {
                eventSystem.SetSelectedGameObject(target);
            }
        }

        private GameObject ResolveTarget()
        {
            if (SelectionUtility.IsValidTarget(_lastValidSelected))
            {
                return _lastValidSelected;
            }

            if (_fallbackSelectable && SelectionUtility.IsValidTarget(_fallbackSelectable.gameObject))
            {
                return _fallbackSelectable.gameObject;
            }

            return null;
        }

        private bool WasNavigationInputThisFrame()
        {
#if ENABLE_INPUT_SYSTEM
            if (_navigationActions != null)
            {
                foreach (InputActionReference reference in _navigationActions)
                {
                    if (reference && reference.action != null && reference.action.WasPerformedThisFrame())
                    {
                        return true;
                    }
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Mathf.Abs(Input.GetAxisRaw(_horizontalAxis)) > _axisThreshold ||
                Mathf.Abs(Input.GetAxisRaw(_verticalAxis)) > _axisThreshold ||
                Input.GetButtonDown(_submitButton))
            {
                return true;
            }
#endif

            return false;
        }
    }
}
