using System.Collections;
using FakeMG.SOEventSystem.EventChannel;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.SOEventSystem.PayloadAdapter {
    public abstract class GenericPayloadAdapter<T> : MonoBehaviour {
        [SerializeField] protected GenericEventChannelSO<T> channel;
        [SerializeField] private float delay;
        [SerializeField] protected UnityEvent<T> onPayload;
        
        protected virtual void OnEnable() {
            if (channel) channel.OnEventRaised += OnEventRaised;
        }

        protected virtual void OnDisable() {
            if (channel) channel.OnEventRaised -= OnEventRaised;
        }
        
        private void OnEventRaised(T payload) {
            StartCoroutine(RaiseEventDelayed(payload));
        }
        
        private IEnumerator RaiseEventDelayed(T payload) {
            yield return new WaitForSeconds(delay);
            onPayload.Invoke(payload);
            HandleEachDataTypeInPayload(payload);
        }
        
        protected abstract void HandleEachDataTypeInPayload(T payload);
    }
}