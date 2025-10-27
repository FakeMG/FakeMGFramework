using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace FakeMG.Framework.UI
{
    public class UIGroupSpawner : MonoBehaviour
    {
        [Header("Prefab Settings")]
        public RectTransform UiPrefab;
        public Canvas TargetCanvas;

        [Header("Multiple Spawn Settings")]
        public int NumberOfObjects = 5;
        public float SpreadRadius = 50f;

        private const float MIN_DISTANCE_BETWEEN_OBJECTS = 30f;
        private const int MAX_POSITION_ATTEMPTS = 50;

        private RectTransform SpawnUI(Vector3 worldPosition)
        {
            RectTransform spawnedUI = Instantiate(UiPrefab, TargetCanvas.transform);

            spawnedUI.gameObject.SetActive(true);
            spawnedUI.transform.position = worldPosition;
            spawnedUI.transform.localScale = Vector3.one;

            return spawnedUI;
        }

        public List<RectTransform> SpawnMultipleUIs(Vector3 centerSpawnPosition)
        {
            List<RectTransform> spawnedUIs = new List<RectTransform>();
            List<Vector3> usedPositions = new List<Vector3>();

            for (int i = 0; i < NumberOfObjects; i++)
            {
                Vector3 randomSpawnPos = GetRandomNonOverlappingPosition(centerSpawnPosition, usedPositions);
                usedPositions.Add(randomSpawnPos);

                RectTransform spawnedUI = SpawnUI(randomSpawnPos);
                spawnedUIs.Add(spawnedUI);
            }

            return spawnedUIs;
        }

        public void SpawnMultipleUIsDelay(Vector3 spawnPosition, float staggerDelay = 0, Action<RectTransform> onSpawned = null)
        {
            StartCoroutine(SpawnMultipleUIsWithDelay(spawnPosition, staggerDelay, onSpawned));
        }

        private IEnumerator SpawnMultipleUIsWithDelay(Vector3 centerSpawnPosition, float delay, Action<RectTransform> onSpawned = null)
        {
            List<Vector3> usedPositions = new List<Vector3>();

            for (int i = 0; i < NumberOfObjects; i++)
            {
                Vector3 randomSpawnPos = GetRandomNonOverlappingPosition(centerSpawnPosition, usedPositions);
                usedPositions.Add(randomSpawnPos);

                RectTransform spawnedUI = SpawnUI(randomSpawnPos);
                onSpawned?.Invoke(spawnedUI);

                yield return new WaitForSeconds(delay);
            }
        }

        private Vector3 GetRandomNonOverlappingPosition(Vector3 center, List<Vector3> usedPositions)
        {
            Vector3 randomPos;
            int attempts = 0;

            do
            {
                Vector2 randomCircle = Random.insideUnitCircle * SpreadRadius;
                randomPos = center + (Vector3)randomCircle;
                attempts++;

                if (attempts >= MAX_POSITION_ATTEMPTS)
                {
                    Debug.LogWarning($"UIGroupSpawner: Could not find a non-overlapping position after {MAX_POSITION_ATTEMPTS} attempts. The spawned UI may overlap with others.");
                    break;
                }
            }
            while (IsPositionTooClose(randomPos, usedPositions, MIN_DISTANCE_BETWEEN_OBJECTS));

            return randomPos;
        }

        private bool IsPositionTooClose(Vector3 position, List<Vector3> existingPositions, float minDistance)
        {
            float minDistanceSqr = minDistance * minDistance;
            foreach (Vector3 existingPos in existingPositions)
            {
                if ((position - existingPos).sqrMagnitude < minDistanceSqr)
                    return true;
            }

            return false;
        }
    }
}