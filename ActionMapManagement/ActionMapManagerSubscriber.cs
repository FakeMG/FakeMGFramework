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
        }

        private void OnDisable()
        {
            EventBus<EnableActionMapEvent>.OnEvent -= OnEnableActionMap;
            EventBus<DisableActionMapEvent>.OnEvent -= OnDisableActionMap;
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
    }
}
