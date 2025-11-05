using Cysharp.Threading.Tasks;
using UnityEngine;

namespace FakeMG.Framework.SceneLoading
{
    public class SceneLoadTrigger : MonoBehaviour
    {
        [SerializeField] private AssetReferenceScene _sceneToLoad;
        [SerializeField] private SceneLoader _sceneLoader;
        [SerializeField] private bool _loadOnStart;
        [SerializeField] private float _delayBeforeLoadSeconds;
        [SerializeField] private bool _setActiveAfterLoad = true;

        private void Start()
        {
            if (_loadOnStart)
            {
                LoadTargetScene().Forget();
            }
        }

        public async UniTaskVoid LoadTargetScene()
        {
            if (_delayBeforeLoadSeconds > 0f)
            {
                await UniTask.Delay(System.TimeSpan.FromSeconds(_delayBeforeLoadSeconds));
            }

            await _sceneLoader.LoadSceneAsync(_sceneToLoad);

            if (_setActiveAfterLoad)
            {
                _sceneLoader.SetActiveScene(_sceneToLoad);
            }
        }
    }
}