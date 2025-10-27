using System.Collections;
using FakeMG.Framework.SOEventSystem.EventChannel;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.Framework.SOEventSystem.PayloadAdapter
{
    public class VoidPayloadAdapter : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO _channel;
        [SerializeField] private float _delay;
        [SerializeField] private UnityEvent _response;

        private void OnEnable()
        {
            if (_channel) _channel.OnEventRaised += OnEventRaised;
        }

        private void OnDisable()
        {
            if (_channel) _channel.OnEventRaised -= OnEventRaised;
        }

        private void OnEventRaised()
        {
            StartCoroutine(RaiseEventDelayed());
        }

        private IEnumerator RaiseEventDelayed()
        {
            yield return new WaitForSeconds(_delay);
            _response?.Invoke();
        }
    }
}