using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework
{
    public class FixedWidthOrthographicCamera : MonoBehaviour
    {
        [SerializeField] private Camera _mainCamera;
        [Tooltip("The width of your level in Unity World Units that must always be visible.")]
        [SerializeField] private float _targetWidth = 10f;

        private void Awake()
        {
            // Orthographic size is half the vertical size of the camera view in world units.
            _mainCamera.orthographicSize = (_targetWidth / _mainCamera.aspect) * 0.5f;
        }

        [Button]
        private void SetTargetWidthToCurrentWidth()
        {
            _targetWidth = _mainCamera.orthographicSize * 2 * _mainCamera.aspect;
            _mainCamera.orthographicSize = _targetWidth / _mainCamera.aspect * 0.5f;
        }
    }
}