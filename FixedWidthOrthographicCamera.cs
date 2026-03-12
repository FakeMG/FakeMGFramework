using Sirenix.OdinInspector;
using UnityEngine;

namespace FakeMG.Framework
{
    public class FixedWidthOrthographicCamera : MonoBehaviour
    {
        [Tooltip("The width of your level in Unity World Units that must always be visible.")]
        [SerializeField] private float _targetWidth = 10f;

        private void Start()
        {
            // Orthographic size is half the vertical size of the camera view in world units.
            Camera cam = Camera.main;
            cam.orthographicSize = (_targetWidth / cam.aspect) * 0.5f;
        }

        [Button]
        private void SetTargetWidthToCurrentWidth()
        {
            Camera cam = Camera.main;
            _targetWidth = cam.orthographicSize * 2 * cam.aspect;
            cam.orthographicSize = _targetWidth / cam.aspect * 0.5f;
        }
    }
}