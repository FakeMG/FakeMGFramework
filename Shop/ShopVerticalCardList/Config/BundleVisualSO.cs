using FakeMG.Framework;
using UnityEngine;

namespace FakeMG.Shop.Config
{
    [CreateAssetMenu(menuName = FakeMGEditorMenus.SHOP + "/BundleVisualSO")]
    public class BundleVisualSO : ScriptableObject
    {
        [SerializeField] private Sprite _bundleBackgroundSprite;
        [SerializeField] private Sprite _headerBackgroundSprite;
        [SerializeField] private Sprite _auraSprite;

        #region Public Properties

        public Sprite BundleBackgroundSprite => _bundleBackgroundSprite;
        public Sprite HeaderBackgroundSprite => _headerBackgroundSprite;
        public Sprite AuraSprite => _auraSprite;

        #endregion
    }
}
