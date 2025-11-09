using FakeMG.Framework.EventBus;
using UnityEngine;

namespace FakeMG.Framework.ActionMapManagement
{
    public class ActionMapManagerSubscriber : MonoBehaviour
    {
        [SerializeField] private ActionMapManager _actionMapManager;

        private void OnEnable()
        {
            EventBus<EnableActionMapEvent>.OnEvent += OnEnableActionMap;
            EventBus<DisableActionMapEvent>.OnEvent += OnDisableActionMap;
            EventBus<PauseAllActionMapsEvent>.OnEvent += OnPauseAllActionMaps;
            EventBus<ResumeAllActionMapsEvent>.OnEvent += OnResumeAllActionMaps;
        }

        private void OnDisable()
        {
            EventBus<EnableActionMapEvent>.OnEvent -= OnEnableActionMap;
            EventBus<DisableActionMapEvent>.OnEvent -= OnDisableActionMap;
            EventBus<PauseAllActionMapsEvent>.OnEvent -= OnPauseAllActionMaps;
            EventBus<ResumeAllActionMapsEvent>.OnEvent -= OnResumeAllActionMaps;
        }

        private void OnEnableActionMap(EnableActionMapEvent evt)
        {
            if (evt.ActionMap)
            {
                _actionMapManager.EnableActionMap(evt.ActionMap.ActionMapName);
            }
        }

        private void OnDisableActionMap(DisableActionMapEvent evt)
        {
            if (evt.ActionMap)
            {
                _actionMapManager.DisableActionMap(evt.ActionMap.ActionMapName);
            }
        }

        private void OnPauseAllActionMaps(PauseAllActionMapsEvent evt)
        {
            _actionMapManager.PauseAllActionMaps();
        }

        private void OnResumeAllActionMaps(ResumeAllActionMapsEvent evt)
        {
            _actionMapManager.ResumeAllActionMaps();
        }
    }
}
