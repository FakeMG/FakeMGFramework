using System;
using System.Collections.Generic;

namespace FakeMG.FakeMGFramework.StateManagement
{
    public class StateMachine
    {
        private IState _currentState;

        private readonly Dictionary<Type, List<Transition>> _transitions = new();
        private List<Transition> _currentStateTransitions = new();
        private readonly List<Transition> _anyTransitions = new();

        private static readonly List<Transition> EmptyTransitions = new(0);

        public void Tick()
        {
            var transition = GetTransition();
            if (transition != null)
            {
                SetState(transition.To);
            }

            _currentState?.Tick();
        }

        public void SetState(IState state)
        {
            if (state == _currentState) return;

            _currentState?.OnExit();
            _currentState = state;

            _transitions.TryGetValue(_currentState.GetType(), out _currentStateTransitions);
            if (_currentStateTransitions == null)
                _currentStateTransitions = EmptyTransitions;

            _currentState.OnEnter();
        }

        public void AddTransition(IState from, IState to, Func<bool> predicate)
        {
            if (_transitions.TryGetValue(from.GetType(), out var transitions) == false)
            {
                transitions = new List<Transition>();
                _transitions[from.GetType()] = transitions;
            }

            transitions.Add(new Transition(to, predicate));
        }

        public void AddAnyTransition(IState state, Func<bool> predicate)
        {
            _anyTransitions.Add(new Transition(state, predicate));
        }

        private Transition GetTransition()
        {
            foreach (var transition in _anyTransitions)
            {
                if (transition.Condition()) return transition;
            }

            foreach (var transition in _currentStateTransitions)
            {
                if (transition.Condition()) return transition;
            }

            return null;
        }

        public void ExitCurrentState()
        {
            _currentState?.OnExit();
            _currentState = null;
        }

        private class Transition
        {
            public Func<bool> Condition { get; }
            public IState To { get; }

            public Transition(IState to, Func<bool> condition)
            {
                To = to;
                Condition = condition;
            }
        }
    }
}