using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace FakeMG.Framework
{
    public class DragObject : MonoBehaviour
    {
        private static DragObject s_instance;

        public static DragObject Instance
        {
            get
            {
                if (s_instance) return s_instance;

                var instances = FindObjectsByType<DragObject>(FindObjectsSortMode.None);
                if (instances.Length > 0)
                {
                    return instances[0];
                }

                return null;
            }
            private set => s_instance = value;
        }

        [SerializeField] private LayerMask _dragLayerMask;
        [SerializeField] private float _dragHeight = 1f;
        public UnityEvent<GameObject> OnSelectEvent;
        public UnityEvent<GameObject> OnReleaseEvent;

        public bool IsDragging { get; private set; }
        private Rigidbody _selectedRigidbody;
        private Transform _selectedTransform;
        private Vector3 _lastObjectPosition;

        private Camera _mainCamera;
        private InputAction _selectAction;
        private InputAction _pointerMovementAction;

        private Plane _dragPlane;

        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            _selectAction = InputSystem.actions["Select"];
            _pointerMovementAction = InputSystem.actions["Pointer Movement"];

            _selectAction.performed += OnSelect;
            _selectAction.canceled += OnRelease;
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
            if (Instance == this)
            {
                Instance = null;
            }

            _selectAction.performed -= OnSelect;
            _selectAction.canceled -= OnRelease;
        }

        private void OnSelect(InputAction.CallbackContext context)
        {
            Ray ray = _mainCamera.ScreenPointToRay(_pointerMovementAction.ReadValue<Vector2>());
            if (Physics.Raycast(ray, out RaycastHit hit, 100, _dragLayerMask))
            {
                IsDragging = true;
                _selectedTransform = hit.transform;
                _dragPlane = new Plane(Vector3.up, new Vector3(0, 0, 0));

                if (hit.collider.TryGetComponent(out _selectedRigidbody))
                {
                    _selectedRigidbody.isKinematic = true;
                }

                OnSelectEvent.Invoke(hit.collider.gameObject);
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

            var releasedObject = _selectedTransform ? _selectedTransform.gameObject : null;
            _selectedTransform = null;

            OnReleaseEvent.Invoke(releasedObject);
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

            Ray ray = _mainCamera.ScreenPointToRay(_pointerMovementAction.ReadValue<Vector2>());

            if (_dragPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                hitPoint.y = _dragHeight;
                _selectedTransform.position = Vector3.Lerp(_selectedTransform.position, hitPoint, 0.1f);
            }
        }

        public bool IsDraggingObject(GameObject checkObject)
        {
            return _selectedTransform && _selectedTransform.gameObject == checkObject;
        }
    }
}