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
            _actionMapManager.EnableActionMap(evt.ActionMapName);
        }

        private void OnDisableActionMap(DisableActionMapEvent evt)
        {
            _actionMapManager.DisableActionMap(evt.ActionMapName);
        }
    }
}
