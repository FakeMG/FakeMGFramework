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
        [SerializeField] private FortuneWheelGameLogic _fortuneWheelGameLogic;
        [SerializeField] private FortuneWheelNotifier _fortuneWheelNotifier;
        [SerializeField] private GachaSystem _gachaSystem;
        [SerializeField] private ItemClaimPopupAnimator _rewardClaimPopupAnimator;
        [SerializeField] private ItemIconUIUpdater _rewardItemUIUpdaterPrefab;

        [Header("Visual Settings")]
        [SerializeField] private float _spinDuration = 3f;
        [SerializeField] private float _itemIconRadius = 200f;
        [SerializeField] private Transform _itemIconContainer;
        [SerializeField] private Transform _wheel;

        [Header("Buttons")]
        [SerializeField] private Button _spinButton;
        [SerializeField] private Button _spinWithAdButton;
        [SerializeField] private Button _exitButton;

        [Header("Visual Effects")]
        // [SerializeField] private ParticleSystem confettiParticleSystem;
        [SerializeField] private AudioCue _claimAudioCue;
        [SerializeField] private AudioCue _spinAudioCue;

        [Header("Cooldown UI")]
        [SerializeField] private TextMeshProUGUI _coolDownText;

        private bool _isRotating;
        private const float MIN_ROTATIONS = 2f;

        private float _segmentAngle;

        private void Start()
        {
            UpdateSegmentAngle();
            UpdateCooldownUI();

            // Subscribe to game logic events
            _fortuneWheelGameLogic.OnSpinStarted += Rotate;

            // Init reward items
            for (int rewardIndex = 0; rewardIndex < _gachaSystem.Rewards.Count; rewardIndex++)
            {
                var reward = _gachaSystem.Rewards[rewardIndex];
                if (!reward.RewardObject) continue;
                // Spawn item icon UI and put it in the container to form an anticlockwise circle.
                // The first item will be at the top.
                // The second item will be at the left of the first item, and so on.
                var itemIconUI = Instantiate(_rewardItemUIUpdaterPrefab, _itemIconContainer);
                itemIconUI.UpdateUIAsync(reward.RewardObject, reward.Amount).Forget();

                float angle = GetRewardAngle(rewardIndex);
                float angleInRadians = angle * Mathf.Deg2Rad;

                Vector3 position = new Vector3(
                    Mathf.Sin(angleInRadians) * _itemIconRadius,
                    Mathf.Cos(angleInRadians) * _itemIconRadius,
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
            _fortuneWheelGameLogic.OnSpinStarted -= Rotate;
        }

        private void UpdateCooldownUI()
        {
            if (_fortuneWheelGameLogic.IsInCooldown())
            {
                _spinButton.interactable = false;
                TimeSpan remainingTime = _fortuneWheelGameLogic.GetRemainingCooldownTime();
                int minutes = Mathf.FloorToInt((float)remainingTime.TotalMinutes);
                int seconds = remainingTime.Seconds;
                _coolDownText.text = $"{minutes:00}:{seconds:00}";
            }
            else
            {
                _spinButton.interactable = true;
                _coolDownText.text = "FREE SPIN";
            }
        }

        private void Rotate()
        {
            if (_isRotating) return;

            // Hide buttons
            _spinButton.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            _spinWithAdButton.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
            _exitButton.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);

            _spinAudioCue.PlayAudioCue();

            _isRotating = true;

            // Get current wheel rotation and normalize it
            float currentRotation = NormalizeAngle(_wheel.eulerAngles.z);

            // Calculate the target angle for the chosen reward
            float targetAngle = NormalizeAngle(GetRewardAngle(_fortuneWheelGameLogic.ChosenRewardIndex));

            // Calculate the shortest rotation needed to reach the target
            float angleDifference = Mathf.DeltaAngle(currentRotation, targetAngle);

            // Add minimum rotations (ensure at least 2 full rotations)
            float totalRotation = MIN_ROTATIONS * 360f + angleDifference;

            _wheel.DORotate(new Vector3(0, 0, _wheel.eulerAngles.z + totalRotation), _spinDuration,
                    RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuart)
                .OnComplete(() =>
                {
                    _spinAudioCue.StopAudioCue();
                    _isRotating = false;
                    ShowRewardPopup(_fortuneWheelGameLogic.ChosenRewardIndex);
                });
        }

        private void ShowRewardPopup(int rewardIndex)
        {
            // confettiParticleSystem.Play();
            _claimAudioCue.PlayAudioCue();

            _rewardClaimPopupAnimator.UnsubscribeAllFromClaimButton();
            _rewardClaimPopupAnimator.SubscribeToClaimButton(multiplier =>
            {
                // confettiParticleSystem.Stop();
                _fortuneWheelGameLogic.GrantReward(multiplier);

                _spinButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                _spinWithAdButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                _exitButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
            });

            var rewardItems = new Dictionary<ItemSO, int>
            {
                { _gachaSystem.Rewards[rewardIndex].RewardObject, _gachaSystem.Rewards[rewardIndex].Amount }
            };
            _rewardClaimPopupAnimator.SetRewards(rewardItems);
            // TODO: use the PopupsManager to show the popup instead of directly calling Show()
            _rewardClaimPopupAnimator.Show().Forget();
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
            _segmentAngle = 360f / _gachaSystem.Rewards.Count;
        }
    }
}