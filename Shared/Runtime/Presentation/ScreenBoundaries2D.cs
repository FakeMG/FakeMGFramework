using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeMG.Framework
{
    [Flags]
    public enum ScreenSide
    {
        None = 0,
        Left = 1 << 0,
        Top = 1 << 1,
        Right = 1 << 2,
        Bottom = 1 << 3,
        All = Left | Top | Right | Bottom
    }

    public class ScreenBoundaries2D : MonoBehaviour
    {
        [SerializeField] private ScreenSide _activeSides = ScreenSide.All;
        [SerializeField] private Camera _mainCamera;
        [SerializeField] private PhysicsMaterial2D _physicsMaterial;

        private readonly List<EdgeCollider2D> _edgeColliders = new();

        private void Start()
        {
            UpdateBoundaries();
        }

#if UNITY_EDITOR
        // Only update every frame inside the Unity Editor to handle resizing the Game View.
        // On a mobile device, the screen resolution doesn't change during gameplay.
        private void Update()
        {
            UpdateBoundaries();
        }
#endif

        private void UpdateBoundaries()
        {
            if (!_mainCamera) return;

            Vector2 bottomLeft = _mainCamera.ViewportToWorldPoint(new Vector3(0, 0, _mainCamera.nearClipPlane));
            Vector2 topLeft = _mainCamera.ViewportToWorldPoint(new Vector3(0, 1, _mainCamera.nearClipPlane));
            Vector2 topRight = _mainCamera.ViewportToWorldPoint(new Vector3(1, 1, _mainCamera.nearClipPlane));
            Vector2 bottomRight = _mainCamera.ViewportToWorldPoint(new Vector3(1, 0, _mainCamera.nearClipPlane));

            var segments = new List<List<Vector2>>();
            List<Vector2> currentSegment = null;

            TryAddEdge(ScreenSide.Left, bottomLeft, topLeft, ref currentSegment, segments);
            TryAddEdge(ScreenSide.Top, topLeft, topRight, ref currentSegment, segments);
            TryAddEdge(ScreenSide.Right, topRight, bottomRight, ref currentSegment, segments);
            TryAddEdge(ScreenSide.Bottom, bottomRight, bottomLeft, ref currentSegment, segments);

            if (currentSegment != null)
                segments.Add(currentSegment);

            ApplySegmentsToColliders(segments);
        }

        private void TryAddEdge(ScreenSide side, Vector2 start, Vector2 end, ref List<Vector2> currentSegment, List<List<Vector2>> segments)
        {
            if (!HasSide(side))
            {
                if (currentSegment != null)
                {
                    segments.Add(currentSegment);
                    currentSegment = null;
                }
                return;
            }

            if (currentSegment == null)
            {
                currentSegment = new List<Vector2> { start };
            }

            currentSegment.Add(end);
        }

        private void ApplySegmentsToColliders(List<List<Vector2>> segments)
        {
            while (_edgeColliders.Count < segments.Count)
                _edgeColliders.Add(gameObject.AddComponent<EdgeCollider2D>());

            for (int i = 0; i < _edgeColliders.Count; i++)
            {
                if (i < segments.Count)
                {
                    _edgeColliders[i].enabled = true;
                    _edgeColliders[i].points = segments[i].ToArray();
                    _edgeColliders[i].sharedMaterial = _physicsMaterial;
                }
                else
                {
                    _edgeColliders[i].enabled = false;
                }
            }
        }

        private bool HasSide(ScreenSide side) => (_activeSides & side) != 0;
    }
}