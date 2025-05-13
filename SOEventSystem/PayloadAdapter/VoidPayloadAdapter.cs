using System.Collections;
using FakeMG.FakeMGFramework.SOEventSystem.EventChannel;
using UnityEngine;
using UnityEngine.Events;

namespace FakeMG.FakeMGFramework.SOEventSystem.PayloadAdapter {
    public class VoidPayloadAdapter : MonoBehaviour {
        [SerializeField] private VoidEventChannelSO channel;
        [SerializeField] private float delay;
        [SerializeField] private UnityEvent response;

        private void OnEnable() {
            if (channel) channel.OnEventRaised += OnEventRaised;
        }

        private void OnDisable() {
            if (channel) channel.OnEventRaised -= OnEventRaised;
        }

        private void OnEventRaised() {
            StartCoroutine(RaiseEventDelayed());
        }

        private IEnumerator RaiseEventDelayed() {
            yield return new WaitForSeconds(delay);
            response?.Invoke();
        }
    }
}