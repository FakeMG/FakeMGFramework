using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace FakeMG.Framework.UI
{
    public sealed class IdentityIconView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;

        private readonly AddressableIconCache _iconCache = new();
        private int _requestVersion;

        public Sprite DisplayedSprite => _iconImage.sprite;

        #region Unity Lifecycle

        private void OnDestroy()
        {
            ++_requestVersion;
            _iconCache.Dispose();
        }

        #endregion

        #region Public Methods

        public async UniTask SetIdentityAsync(IdentitySO identitySO, CancellationToken cancellationToken,
            AddressableIconCache sharedIconCache = null)
        {
            int requestVersion = ++_requestVersion;
            _iconImage.sprite = null;
            AddressableIconCache iconCache = sharedIconCache ?? _iconCache;
            Sprite sprite = await iconCache.LoadIconAsync(identitySO, cancellationToken);
            if (requestVersion == _requestVersion)
            {
                _iconImage.sprite = sprite;
            }
        }

        public void Clear()
        {
            ++_requestVersion;
            _iconImage.sprite = null;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(Image iconImage)
        {
            _iconImage = iconImage;
        }
#endif

        #endregion
    }
}
