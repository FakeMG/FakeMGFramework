using System.Collections;
using FakeMG.Framework.SOEventSystem.EventChannel;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.Framework.SOEventSystem.PayloadAdapter
{
    public abstract class GenericPayloadAdapter<T> : MonoBehaviour
    {
        [SerializeField] protected GenericEventChannelSO<T> _channel;
        [SerializeField] private float _delay;
        [SerializeField] protected UnityEvent<T> _onPayload;

        protected virtual void OnEnable()
        {
            if (_channel) _channel.OnEventRaised += OnEventRaised;
        }

        protected virtual void OnDisable()
        {
            if (_channel) _channel.OnEventRaised -= OnEventRaised;
        }

        private void OnEventRaised(T payload)
        {
            StartCoroutine(RaiseEventDelayed(payload));
        }

        private IEnumerator RaiseEventDelayed(T payload)
        {
            yield return new WaitForSeconds(_delay);
            _onPayload.Invoke(payload);
            HandleEachDataTypeInPayload(payload);
        }

        protected abstract void HandleEachDataTypeInPayload(T payload);
    }
}