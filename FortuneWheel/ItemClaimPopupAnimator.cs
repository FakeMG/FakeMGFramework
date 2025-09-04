using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using FakeMG.Framework.UI;
using FakeMG.Framework.UI.Popup;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.FortuneWheel
{
    public class ItemClaimPopupAnimator : PopupAnimator
    {
        [SerializeField] private ItemIconUIUpdater itemUIUpdaterPrefab;
        [SerializeField] private Transform gridLayoutContainer;
        [SerializeField] private Button claimButton;
        [SerializeField] private Button claimWithAdButton;
        [SerializeField] private Image auraImage;
        [SerializeField] private Transform x2Text;

        private Vector3 _initialTextScale; // used in the x2 text animation
        private Dictionary<ItemSO, int> _rewardItems;

        private void Start()
        {
            _initialTextScale = x2Text.localScale;
        }

        private void SetUpInitialState()
        {
            canvasGroup.gameObject.SetActive(false);
            canvasGroup.alpha = 0f;

            claimButton.transform.localScale = Vector3.zero;
            claimButton.interactable = true;
            claimWithAdButton.transform.localScale = Vector3.zero;
            claimWithAdButton.interactable = true;

            auraImage.transform.DOKill();
            auraImage.gameObject.SetActive(false);
            auraImage.transform.localScale = Vector3.zero;

            x2Text.localScale = Vector3.zero;
            x2Text.gameObject.SetActive(false);
        }

        public void SetRewards(Dictionary<ItemSO, int> rewardItems)
        {
            _rewardItems = rewardItems;
        }

        protected override Sequence CreateShowSequence()
        {
            // Clear existing items
            foreach (Transform child in gridLayoutContainer.transform)
            {
                Destroy(child.gameObject);
            }

            auraImage.gameObject.SetActive(true);
            auraImage.transform.DOScale(Vector3.one, 0.5f)
                .SetEase(Ease.OutBack)
                .SetLink(auraImage.gameObject);
            auraImage.DOFade(1f, 0.3f)
                .SetLink(auraImage.gameObject);
            auraImage.transform.DORotate(new Vector3(0, 0, 360), 4f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1)
                .SetLink(auraImage.gameObject);

            var sequence = DOTween.Sequence();

            sequence.Join(canvasGroup.DOFade(1f, 0.3f)
                .SetEase(Ease.OutCubic)
                .SetLink(canvasGroup.gameObject));

            // Populate with new items
            foreach (var kvp in _rewardItems)
            {
                var itemUI = Instantiate(itemUIUpdaterPrefab, gridLayoutContainer.transform);
                itemUI.transform.SetAsFirstSibling();
                itemUI.UpdateUIAsync(kvp.Key, kvp.Value).Forget();
                itemUI.transform.localScale = Vector3.zero; // Start with scale zero

                // Animate the item into view
                sequence.Append(itemUI.transform.DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutBack)
                    .SetLink(itemUI.gameObject));
            }

            sequence.SetDelay(0.2f);
            sequence.OnComplete(() =>
            {
                claimButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                claimWithAdButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
            });

            return sequence;
        }

        protected override Sequence CreateHideSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Join(canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InCubic)
                .SetLink(canvasGroup.gameObject)
                .OnComplete(() =>
                {
                    SetUpInitialState();
                    foreach (Transform child in gridLayoutContainer.transform)
                    {
                        Destroy(child.gameObject);
                    }
                }));

            return sequence;
        }

        protected override void ShowImmediate()
        {
            // Clear existing items
            foreach (Transform child in gridLayoutContainer.transform)
            {
                Destroy(child.gameObject);
            }

            canvasGroup.gameObject.SetActive(true);
            canvasGroup.alpha = 1f;

            auraImage.gameObject.SetActive(true);
            auraImage.transform.localScale = Vector3.one;
            auraImage.color = new Color(auraImage.color.r, auraImage.color.g, auraImage.color.b, 1f);
            auraImage.transform.DORotate(new Vector3(0, 0, 360), 4f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1)
                .SetLink(auraImage.gameObject);

            foreach (var kvp in _rewardItems)
            {
                var itemUI = Instantiate(itemUIUpdaterPrefab, gridLayoutContainer.transform);
                itemUI.transform.SetAsFirstSibling();
                itemUI.UpdateUIAsync(kvp.Key, kvp.Value).Forget();
                itemUI.transform.localScale = Vector3.one; // Set to one for immediate visibility
            }

            claimButton.transform.localScale = Vector3.one;
            claimWithAdButton.transform.localScale = Vector3.one;
        }

        protected override void HideImmediate()
        {
            SetUpInitialState();

            foreach (Transform child in gridLayoutContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        public void UnsubscribeAllFromClaimButton()
        {
            claimButton.onClick.RemoveAllListeners();
            claimWithAdButton.onClick.RemoveAllListeners();
        }

        public void SubscribeToClaimButton(Action<int> callback)
        {
            claimButton.onClick.AddListener(() =>
            {
                callback?.Invoke(1);
                claimButton.interactable = false;
                claimWithAdButton.interactable = false;
                Hide().Forget();
            });

            claimWithAdButton.onClick.AddListener(() =>
            {
                // TODO: Implement ad logic
                // BounceAdsSdk.ShowRewarded(success =>
                // {
                //     if (success)
                //     {
                //         x2Text.gameObject.SetActive(true);
                //         x2Text.DOScale(_initialTextScale, 0.2f).SetEase(Ease.OutBack).SetLink(x2Text.gameObject);
                //         DOVirtual.DelayedCall(1f, Hide);
                //         claimButton.interactable = false;
                //         claimWithAdButton.interactable = false;
                //         callback?.Invoke(2);
                //     }
                // }, "daily_reward_claimX2", "item_id", "?");
            });
        }
    }
}