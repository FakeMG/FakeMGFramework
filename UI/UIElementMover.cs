using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace FakeMG.FakeMGFramework.UI
{
    public class UIElementMover : MonoBehaviour
    {
        [Header("Prefab Settings")]
        public RectTransform uiPrefab;
        public Canvas targetCanvas;

        [Header("Animation Settings")]
        public float scaleDuration = 0.5f;
        public float moveDuration = 1.0f;
        public Ease scaleEase = Ease.OutBack;
        public Ease moveEase = Ease.InOutQuart;

        [Header("Multiple Spawn Settings")]
        public int numberOfObjects = 5;
        public float spreadRadius = 50f;

        public void SpawnAndMoveMultipleUIs(Vector3 spawnPosition, Vector3 targetPosition, float staggerDelay = 0)
        {
            StartCoroutine(SpawnUIsWithDelay(spawnPosition, targetPosition, staggerDelay));
        }

        private IEnumerator SpawnUIsWithDelay(Vector3 centerSpawnPosition, Vector3 targetPosition, float delay)
        {
            List<Vector3> usedPositions = new List<Vector3>();

            for (int i = 0; i < numberOfObjects; i++)
            {
                Vector3 randomSpawnPos = GetRandomNonOverlappingPosition(centerSpawnPosition, usedPositions);
                usedPositions.Add(randomSpawnPos);

                SpawnAndMoveUI(randomSpawnPos, targetPosition);
                yield return new WaitForSeconds(delay);
            }
        }

        public void SpawnAndMoveUI(Vector3 spawnPosition, Vector3 targetPosition)
        {
            RectTransform spawnedUI = SpawnUI(spawnPosition);

            Sequence animationSequence = DOTween.Sequence();
            animationSequence.Append(spawnedUI.DOScale(Vector3.one, scaleDuration).SetEase(scaleEase)
                .SetLink(spawnedUI.gameObject));
            animationSequence.Append(spawnedUI.DOMove(targetPosition, moveDuration).SetEase(moveEase)
                .SetLink(spawnedUI.gameObject));

            // Optional: Add callback when animation completes
            animationSequence.OnComplete(() => { Debug.Log("Image animation completed!"); });
        }

        private Vector3 GetRandomNonOverlappingPosition(Vector3 center, List<Vector3> usedPositions)
        {
            Vector3 randomPos;
            int attempts = 0;
            int maxAttempts = 50; // Prevent infinite loop
            float minDistance = 30f; // Minimum distance between objects to avoid overlap

            do
            {
                // Generate random position within circle around center
                Vector2 randomCircle = Random.insideUnitCircle * spreadRadius;
                randomPos = center + new Vector3(randomCircle.x, randomCircle.y, 0);
                attempts++;

                // If we've tried too many times, just use the last position
                if (attempts >= maxAttempts)
                    break;
            }
            while (IsPositionTooClose(randomPos, usedPositions, minDistance));

            return randomPos;
        }

        private bool IsPositionTooClose(Vector3 position, List<Vector3> existingPositions, float minDistance)
        {
            foreach (Vector3 existingPos in existingPositions)
            {
                if (Vector3.Distance(position, existingPos) < minDistance)
                    return true;
            }

            return false;
        }

        public void SpawnImageWithCustomSettings(
            Vector3 spawnPos, Vector3 targetPos,
            float customScaleDuration, float customMoveDuration,
            Ease customScaleEase, Ease customMoveEase)
        {
            RectTransform spawnedUI = SpawnUI(spawnPos);

            Sequence customSequence = DOTween.Sequence();
            customSequence.Append(spawnedUI.DOScale(Vector3.one, customScaleDuration).SetEase(customScaleEase)
                .SetLink(spawnedUI.gameObject));
            customSequence.Append(spawnedUI.DOMove(targetPos, customMoveDuration).SetEase(customMoveEase)
                .SetLink(spawnedUI.gameObject));
        }

        public void SpawnUIWithSimultaneousAnimation(Vector3 spawnPosition, Vector3 targetPosition)
        {
            RectTransform spawnedImage = SpawnUI(spawnPosition);

            // Scale and move simultaneously
            spawnedImage.DOScale(Vector3.one, scaleDuration).SetEase(scaleEase).SetLink(spawnedImage.gameObject);
            spawnedImage.DOMove(targetPosition, moveDuration).SetEase(moveEase).SetLink(spawnedImage.gameObject);
        }

        private RectTransform SpawnUI(Vector3 worldPosition)
        {
            RectTransform spawnedUI = Instantiate(uiPrefab, targetCanvas.transform);

            spawnedUI.gameObject.SetActive(true);
            spawnedUI.position = worldPosition;
            spawnedUI.localScale = Vector3.zero;

            return spawnedUI;
        }
    }
}