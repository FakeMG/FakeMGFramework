using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FakeMG.Framework.Audio;
using FakeMG.Framework.Gacha;
using FakeMG.Framework.UI;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.FortuneWheel
{
    public class FortuneWheelVisual : MonoBehaviour
    {
        [Header("Fortune Wheel")]
        [SerializeField] private FortuneWheelGameLogic fortuneWheelGameLogic;
        [SerializeField] private FortuneWheelNotifier fortuneWheelNotifier;
        [SerializeField] private GachaSystem gachaSystem;
        [SerializeField] private ItemClaimPopupAnimator rewardClaimPopupAnimator;
        [SerializeField] private ItemIconUIUpdater rewardItemUIUpdaterPrefab;

        [Header("Visual Settings")]
        [SerializeField] private float spinDuration = 3f;
        [SerializeField] private float itemIconRadius = 200f;
        [SerializeField] private Transform itemIconContainer;
        [SerializeField] private Transform wheel;

        [Header("Buttons")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button spinWithAdButton;
        [SerializeField] private Button exitButton;

        [Header("Visual Effects")]
        // [SerializeField] private ParticleSystem confettiParticleSystem;
        [SerializeField] private AudioCue claimAudioCue;
        [SerializeField] private AudioCue spinAudioCue;

        [Header("Cooldown UI")]
        [SerializeField] private TextMeshProUGUI coolDownText;

        private bool _isRotating;
        private const float MIN_ROTATIONS = 2f;

        private float _segmentAngle;

        private void Start()
        {
            UpdateSegmentAngle();
            UpdateCooldownUI();

            // Subscribe to game logic events
            fortuneWheelGameLogic.OnSpinStarted += Rotate;

            // Init reward items
            for (int rewardIndex = 0; rewardIndex < gachaSystem.Rewards.Count; rewardIndex++)
            {
                var reward = gachaSystem.Rewards[rewardIndex];
                if (!reward.rewardObject) continue;
                // Spawn item icon UI and put it in the container to form an anticlockwise circle.
                // The first item will be at the top.
                // The second item will be at the left of the first item, and so on.
                var itemIconUI = Instantiate(rewardItemUIUpdaterPrefab, itemIconContainer);
                itemIconUI.UpdateUIAsync(reward.rewardObject, reward.amount).Forget();

                float angle = GetRewardAngle(rewardIndex);
                float angleInRadians = angle * Mathf.Deg2Rad;

                Vector3 position = new Vector3(
                    Mathf.Sin(angleInRadians) * itemIconRadius,
                    Mathf.Cos(angleInRadians) * itemIconRadius,
                    0f
                );

                // Rotate item to face outward from center (angle + 90 degrees for outward orientation)
                itemIconUI.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0, 0, -angle));
            }
        }

        private void Update()
        {
            UpdateCooldownUI();
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            fortuneWheelGameLogic.OnSpinStarted -= Rotate;
        }

        private void UpdateCooldownUI()
        {
            if (fortuneWheelGameLogic.IsInCooldown())
            {
                spinButton.interactable = false;
                TimeSpan remainingTime = fortuneWheelGameLogic.GetRemainingCooldownTime();
                int minutes = Mathf.FloorToInt((float)remainingTime.TotalMinutes);
                int seconds = remainingTime.Seconds;
                coolDownText.text = $"{minutes:00}:{seconds:00}";
            }
            else
            {
                spinButton.interactable = true;
                coolDownText.text = "FREE SPIN";
            }
        }

        private void Rotate()
        {
            if (_isRotating) return;

            // Hide buttons
            spinButton.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            spinWithAdButton.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            exitButton.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);

            spinAudioCue.PlayAudioCue();

            _isRotating = true;

            // Get current wheel rotation and normalize it
            float currentRotation = NormalizeAngle(wheel.eulerAngles.z);

            // Calculate the target angle for the chosen reward
            float targetAngle = NormalizeAngle(GetRewardAngle(fortuneWheelGameLogic.ChosenRewardIndex));

            // Calculate the shortest rotation needed to reach the target
            float angleDifference = Mathf.DeltaAngle(currentRotation, targetAngle);

            // Add minimum rotations (ensure at least 2 full rotations)
            float totalRotation = MIN_ROTATIONS * 360f + angleDifference;

            wheel.DORotate(new Vector3(0, 0, wheel.eulerAngles.z + totalRotation), spinDuration,
                    RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuart)
                .OnComplete(() =>
                {
                    spinAudioCue.StopAudioCue();
                    _isRotating = false;
                    ShowRewardPopup(fortuneWheelGameLogic.ChosenRewardIndex);
                });
        }

        private void ShowRewardPopup(int rewardIndex)
        {
            // confettiParticleSystem.Play();
            claimAudioCue.PlayAudioCue();

            rewardClaimPopupAnimator.UnsubscribeAllFromClaimButton();
            rewardClaimPopupAnimator.SubscribeToClaimButton(multiplier =>
            {
                // confettiParticleSystem.Stop();
                fortuneWheelGameLogic.GrantReward(multiplier);

                spinButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                spinWithAdButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                exitButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
            });

            var rewardItems = new Dictionary<ItemSO, int>
            {
                { gachaSystem.Rewards[rewardIndex].rewardObject, gachaSystem.Rewards[rewardIndex].amount }
            };
            rewardClaimPopupAnimator.SetRewards(rewardItems);
            // TODO: use the PopupsManager to show the popup instead of directly calling Show()
            rewardClaimPopupAnimator.Show().Forget();
        }

        private float GetRewardAngle(int rewardIndex)
        {
            return rewardIndex * -_segmentAngle;
        }

        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0) angle += 360f;
            return angle;
        }

        [Button]
        private void UpdateSegmentAngle()
        {
            _segmentAngle = 360f / gachaSystem.Rewards.Count;
        }
    }
}