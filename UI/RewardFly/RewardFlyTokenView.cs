using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI.RewardFly
{
    public class RewardFlyTokenView : MonoBehaviour
    {
        [SerializeField] private RectTransform _tokenRectTransform;
        [SerializeField] private Image _tokenIconImage;
        [SerializeField] private SpriteRenderer _tokenSpriteRenderer;

        public Transform FlyTransform => _tokenRectTransform ? _tokenRectTransform : transform;

        #region Public Methods

        public void SetRewardSprite(Sprite rewardSprite)
        {
            if (_tokenIconImage)
            {
                _tokenIconImage.sprite = rewardSprite;
            }

            if (_tokenSpriteRenderer)
            {
                _tokenSpriteRenderer.sprite = rewardSprite;
            }
        }

        public void SetCanvasLocalPosition(Vector3 localPosition)
        {
            FlyTransform.localPosition = localPosition;
        }

        public void SetWorldPosition(Vector3 worldPosition)
        {
            FlyTransform.position = worldPosition;
        }

        public UniTask PlaySpawnScaleAsync(float scaleDurationSeconds)
        {
            FlyTransform.localScale = Vector3.zero;
            Tween scaleTween = FlyTransform.DOScale(Vector3.one, scaleDurationSeconds)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);

            return scaleTween.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        }

        public void Dispose()
        {
            FlyTransform.DOKill();
            Destroy(gameObject);
        }

        #endregion
    }
}
