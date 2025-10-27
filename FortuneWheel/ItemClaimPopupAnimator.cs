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
        [SerializeField] private ItemIconUIUpdater _itemUIUpdaterPrefab;
        [SerializeField] private Transform _gridLayoutContainer;
        [SerializeField] private Button _claimButton;
        [SerializeField] private Button _claimWithAdButton;
        [SerializeField] private Image _auraImage;
        [SerializeField] private Transform _x2Text;

        private Vector3 _initialTextScale; // used in the x2 text animation
        private Dictionary<ItemSO, int> _rewardItems;

        public event Action<Action> OnClaimWithAdRequested;

        private void Start()
        {
            _initialTextScale = _x2Text.localScale;
        }

        private void SetUpInitialState()
        {
            _canvasGroup.gameObject.SetActive(false);
            _canvasGroup.alpha = 0f;

            _claimButton.transform.localScale = Vector3.zero;
            _claimButton.interactable = true;
            _claimWithAdButton.transform.localScale = Vector3.zero;
            _claimWithAdButton.interactable = true;

            _auraImage.transform.DOKill();
            _auraImage.gameObject.SetActive(false);
            _auraImage.transform.localScale = Vector3.zero;

            _x2Text.localScale = Vector3.zero;
            _x2Text.gameObject.SetActive(false);
        }

        public void SetRewards(Dictionary<ItemSO, int> rewardItems)
        {
            _rewardItems = rewardItems;
        }

        protected override Sequence CreateShowSequence()
        {
            // Clear existing items
            foreach (Transform child in _gridLayoutContainer.transform)
            {
                Destroy(child.gameObject);
            }

            _auraImage.gameObject.SetActive(true);
            _auraImage.transform.DOScale(Vector3.one, 0.5f)
                .SetEase(Ease.OutBack)
                .SetLink(_auraImage.gameObject);
            _auraImage.DOFade(1f, 0.3f)
                .SetLink(_auraImage.gameObject);
            _auraImage.transform.DORotate(new Vector3(0, 0, 360), 4f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1)
                .SetLink(_auraImage.gameObject);

            var sequence = DOTween.Sequence();

            sequence.Join(_canvasGroup.DOFade(1f, 0.3f)
                .SetEase(Ease.OutCubic)
                .SetLink(_canvasGroup.gameObject));

            // Populate with new items
            foreach (var kvp in _rewardItems)
            {
                var itemUI = Instantiate(_itemUIUpdaterPrefab, _gridLayoutContainer.transform);
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
                _claimButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
                _claimWithAdButton.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
            });

            return sequence;
        }

        protected override Sequence CreateHideSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.Join(_canvasGroup.DOFade(0f, 0.3f).SetEase(Ease.InCubic)
                .SetLink(_canvasGroup.gameObject)
                .OnComplete(() =>
                {
                    SetUpInitialState();
                    foreach (Transform child in _gridLayoutContainer.transform)
                    {
                        Destroy(child.gameObject);
                    }
                }));

            return sequence;
        }

        protected override void ShowImmediate()
        {
            // Clear existing items
            foreach (Transform child in _gridLayoutContainer.transform)
            {
                Destroy(child.gameObject);
            }

            _canvasGroup.gameObject.SetActive(true);
            _canvasGroup.alpha = 1f;

            _auraImage.gameObject.SetActive(true);
            _auraImage.transform.localScale = Vector3.one;
            _auraImage.color = new Color(_auraImage.color.r, _auraImage.color.g, _auraImage.color.b, 1f);
            _auraImage.transform.DORotate(new Vector3(0, 0, 360), 4f, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1)
                .SetLink(_auraImage.gameObject);

            foreach (var kvp in _rewardItems)
            {
                var itemUI = Instantiate(_itemUIUpdaterPrefab, _gridLayoutContainer.transform);
                itemUI.transform.SetAsFirstSibling();
                itemUI.UpdateUIAsync(kvp.Key, kvp.Value).Forget();
                itemUI.transform.localScale = Vector3.one; // Set to one for immediate visibility
            }

            _claimButton.transform.localScale = Vector3.one;
            _claimWithAdButton.transform.localScale = Vector3.one;
        }

        protected override void HideImmediate()
        {
            SetUpInitialState();

            foreach (Transform child in _gridLayoutContainer.transform)
            {
                Destroy(child.gameObject);
            }
        }

        public void UnsubscribeAllFromClaimButton()
        {
            _claimButton.onClick.RemoveAllListeners();
            _claimWithAdButton.onClick.RemoveAllListeners();
        }

        public void SubscribeToClaimButton(Action<int> callback)
        {
            _claimButton.onClick.AddListener(() =>
            {
                int multiplier = 1;
                callback?.Invoke(multiplier);
                _claimButton.interactable = false;
                _claimWithAdButton.interactable = false;
                Hide().Forget();
            });

            _claimWithAdButton.onClick.AddListener(() =>
            {
                OnClaimWithAdRequested?.Invoke(() =>
                {
                    _x2Text.gameObject.SetActive(true);
                    _x2Text.DOScale(_initialTextScale, 0.2f).SetEase(Ease.OutBack).SetLink(_x2Text.gameObject);
                    DOVirtual.DelayedCall(1f, () => Hide().Forget());
                    _claimButton.interactable = false;
                    _claimWithAdButton.interactable = false;
                    int multiplier = 2;
                    callback?.Invoke(multiplier);
                });
            });
        }
    }
}