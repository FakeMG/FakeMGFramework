using UnityEngine;
using UnityEngine.InputSystem;

namespace FakeMG.Framework.Drag
{
    public class DragObject : MonoBehaviour
    {
        [SerializeField] private LayerMask _dragLayerMask;
        [SerializeField] private float _dragHeight = 1f;
        [SerializeField] private InputActionReference _selectActionReference;
        [SerializeField] private InputActionReference _pointerMovementActionReference;

        private Rigidbody _selectedRigidbody;
        private Transform _selectedTransform;
        private Draggable _selectedDraggable;
        private Vector3 _lastObjectPosition;

        private Camera _mainCamera;
        private Plane _dragPlane;

        public event System.Action<GameObject> OnSelectEvent;
        public event System.Action<GameObject> OnReleaseEvent;

        public bool IsDragging { get; private set; }

        private void Start()
        {
            _mainCamera = Camera.main;

            _selectActionReference.action.performed += OnSelect;
            _selectActionReference.action.canceled += OnRelease;
        }

        private void Update()
        {
            if (IsDragging)
            {
                Drag();
            }
        }

        private void OnDestroy()
        {
            _selectActionReference.action.performed -= OnSelect;
            _selectActionReference.action.canceled -= OnRelease;
        }

        private void OnSelect(InputAction.CallbackContext context)
        {
            Ray ray = _mainCamera.ScreenPointToRay(_pointerMovementActionReference.action.ReadValue<Vector2>());
            if (Physics.Raycast(ray, out RaycastHit hit, 100, _dragLayerMask))
            {
                IsDragging = true;
                _selectedTransform = hit.transform;
                _dragPlane = new Plane(Vector3.up, new Vector3(0, 0, 0));

                hit.collider.TryGetComponent(out _selectedRigidbody);
                if (_selectedRigidbody)
                {
                    _selectedRigidbody.isKinematic = true;
                }

                hit.collider.TryGetComponent(out _selectedDraggable);
                if (_selectedDraggable)
                {
                    _selectedDraggable.StartDrag();
                }

                OnSelectEvent?.Invoke(hit.collider.gameObject);
            }
        }

        private void OnRelease(InputAction.CallbackContext context)
        {
            if (!IsDragging) return;

            IsDragging = false;

            if (_selectedRigidbody)
            {
                _selectedRigidbody.isKinematic = false;
                Throw();
                _selectedRigidbody = null;
            }

            if (_selectedDraggable)
            {
                _selectedDraggable.EndDrag();
                _selectedDraggable = null;
            }

            GameObject releasedObject = _selectedTransform ? _selectedTransform.gameObject : null;
            _selectedTransform = null;

            OnReleaseEvent?.Invoke(releasedObject);
        }

        private void Throw()
        {
            if (!_selectedTransform) return;

            Vector3 throwVector = _selectedTransform.position - _lastObjectPosition;
            float throwSpeed = throwVector.magnitude / Time.deltaTime;
            throwSpeed = Mathf.Clamp(throwSpeed, 0, 20);
            _selectedRigidbody.linearVelocity = throwVector.normalized * throwSpeed;
        }

        private void Drag()
        {
            if (!_selectedTransform) return;

            _lastObjectPosition = _selectedTransform.position;

            Ray ray = _mainCamera.ScreenPointToRay(_pointerMovementActionReference.action.ReadValue<Vector2>());

            if (_dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                hitPoint.y = _dragHeight;
                _selectedTransform.position = Vector3.Lerp(_selectedTransform.position, hitPoint, 0.5f);
            }
        }

        public bool IsDraggingObject(GameObject checkObject)
        {
            return _selectedTransform && _selectedTransform.gameObject == checkObject;
        }
    }
}