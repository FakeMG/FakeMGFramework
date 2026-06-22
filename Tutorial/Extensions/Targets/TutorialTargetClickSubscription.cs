using FakeMG.Framework;
using System;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FakeMG.Tutorial
{
    /// <summary>
    /// Subscribes to a tutorial target's "the player interacted with me" signal without the
    /// caller knowing how the target exposes it: an <see cref="ITutorialActivatableTarget"/>
    /// raises its OnActivated event, any other target falls back to a Button on its
    /// interaction transform. This is the single place that resolves a target's click source,
    /// so pointers and completion conditions react to custom activation events and plain UI
    /// buttons through one path.
    /// </summary>
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
