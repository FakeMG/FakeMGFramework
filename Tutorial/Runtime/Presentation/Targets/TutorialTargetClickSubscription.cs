using FakeMG.Framework;
using System;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FakeMG.Tutorial
{
    public sealed class TutorialTargetClickSubscription
    {
        private readonly Action _onClicked;
        private ITutorialActivatableTarget _activatable;
        private Button _button;
        private UnityAction _buttonListener;

        private TutorialTargetClickSubscription(Action onClicked)
        {
            _onClicked = onClicked;
        }

        public bool IsSubscribed => _activatable != null || _button != null;

        #region Public Methods

        public static TutorialTargetClickSubscription Subscribe(ITutorialTarget target, Action onClicked)
        {
            var subscription = new TutorialTargetClickSubscription(onClicked);
            subscription.SubscribeTo(target);
            return subscription;
        }

        public void Unsubscribe()
        {
            if (_activatable != null)
            {
                if (!_activatable.IsDestroyed())
                {
                    _activatable.OnActivated -= _onClicked;
                }

                _activatable = null;
            }

            if (_button != null)
            {
                _button.onClick.RemoveListener(_buttonListener);
                _button = null;
                _buttonListener = null;
            }
        }

        #endregion

        #region Private Methods

        private void SubscribeTo(ITutorialTarget target)
        {
            if (target is ITutorialActivatableTarget activatable)
            {
                _activatable = activatable;
                _activatable.OnActivated += _onClicked;
                return;
            }

            _button = target.InteractionTransform != null
                ? target.InteractionTransform.GetComponent<Button>()
                : null;
            if (_button == null)
            {
                Echo.Warning($"Tutorial target '{target.Key.name}' exposes no activation event or Button to subscribe to.");
                return;
            }

            _buttonListener = new UnityAction(_onClicked);
            _button.onClick.AddListener(_buttonListener);
        }

        #endregion
    }
}
