using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using FakeMG.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace FakeMG.Framework.UI
{
    public sealed class IdentityIconView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;

        private AsyncOperationHandle<Sprite>? _spriteHandle;
        private int _requestVersion;

        public Sprite DisplayedSprite => _iconImage.sprite;

        #region Unity Lifecycle

        private void OnDestroy()
        {
            ++_requestVersion;
            ReleaseSpriteHandle();
        }

        #endregion

        #region Public Methods

        public async UniTask SetIdentityAsync(IdentitySO identitySO, CancellationToken cancellationToken)
        {
            int requestVersion = ++_requestVersion;
            ReleaseSpriteHandle();
            _iconImage.sprite = null;

            if (identitySO == null || identitySO.IconSpriteAsset == null || !identitySO.IconSpriteAsset.RuntimeKeyIsValid())
            {
                Echo.Warning($"Icon cannot load because identity '{identitySO?.name ?? "<missing>"}' has no valid icon reference.");
                return;
            }

            AsyncOperationHandle<Sprite> spriteHandle = Addressables.LoadAssetAsync<Sprite>(identitySO.IconSpriteAsset);
            try
            {
                await spriteHandle.ToUniTask(cancellationToken: cancellationToken);
                if (spriteHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Echo.Error($"Icon failed to load for identity '{identitySO.name}'.");
                    ReleaseHandle(spriteHandle);
                    return;
                }

                if (requestVersion != _requestVersion)
                {
                    ReleaseHandle(spriteHandle);
                    return;
                }

                _iconImage.sprite = spriteHandle.Result;
                _spriteHandle = spriteHandle;
            }
            catch (OperationCanceledException)
            {
                ReleaseHandle(spriteHandle);
            }
            catch (Exception exception)
            {
                Echo.Error($"Icon load failed with {exception.GetType().Name}: {exception.Message}");
                ReleaseHandle(spriteHandle);
            }
        }

        public void Clear()
        {
            ++_requestVersion;
            ReleaseSpriteHandle();
            _iconImage.sprite = null;
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(Image iconImage)
        {
            _iconImage = iconImage;
        }
#endif

        #endregion

        #region Private Methods

        private void ReleaseSpriteHandle()
        {
            if (_spriteHandle.HasValue)
            {
                ReleaseHandle(_spriteHandle.Value);
                _spriteHandle = null;
            }
        }

        private static void ReleaseHandle(AsyncOperationHandle<Sprite> spriteHandle)
        {
            if (spriteHandle.IsValid())
            {
                Addressables.Release(spriteHandle);
            }
        }

        #endregion
    }
}
