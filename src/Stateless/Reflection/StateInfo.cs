﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Stateless.Reflection
{
    /// <summary>
    /// Describes an internal StateRepresentation through the reflection API.
    /// </summary>
    public class StateInfo
    {
        internal static StateInfo CreateStateInfo<TState, TTrigger>(StateMachine<TState, TTrigger>.StateRepresentation stateReperesentation)
        {
            if (stateReperesentation == null)
                throw new ArgumentException(nameof(stateReperesentation));

            var substates = stateReperesentation.GetSubstates().Select(s => CreateStateInfo(s)).ToList();
            var entryActions = stateReperesentation.EntryActions.Select(e => e.ActionDescription).ToList();
            var exitActions = stateReperesentation.ExitActions.Select(e => e.ActionDescription).ToList();
            var ignoredTriggers = new List<TriggerInfo>();
            StateInfo superstate = null;

            if (stateReperesentation.Superstate != null)
                superstate = CreateStateInfo(stateReperesentation.Superstate);

            foreach (var triggerBehaviours in stateReperesentation.TriggerBehaviours)
            {
                foreach (var item in triggerBehaviours.Value)
                {
                    if (item is StateMachine<TState, TTrigger>.IgnoredTriggerBehaviour)
                        ignoredTriggers.Add(new TriggerInfo(triggerBehaviours.Key, typeof(TTrigger) ));
                }
            }

            return new StateInfo(stateReperesentation.UnderlyingState, typeof(TState), superstate, substates, entryActions, exitActions, ignoredTriggers);
        }

        internal static void AddTransitions<TState, TTrigger>(StateInfo info, StateMachine<TState, TTrigger>.StateRepresentation stateReperesentation, Func<object, StateInfo> lookupState)
        {
            var transitions = new List<TransitionInfo>();
            var internalTransitions = new List<TransitionInfo>();
            var dynamicTransitions = new List<DynamicTransitionInfo>();

            foreach (var triggerBehaviours in stateReperesentation.TriggerBehaviours)
            {
                var triggerText = triggerBehaviours.Key.ToString();

                int unknowns = 0;

                foreach (var item in triggerBehaviours.Value)
                {
                    if (item is StateMachine<TState, TTrigger>.TransitioningTriggerBehaviour)
                        transitions.Add(TransitionInfo.CreateTransitionInfo(triggerBehaviours.Key, (StateMachine<TState, TTrigger>.TransitioningTriggerBehaviour)item, lookupState));

                    if (item is StateMachine<TState, TTrigger>.InternalTriggerBehaviour)
                        transitions.Add(TransitionInfo.CreateInternalTransitionInfo(triggerBehaviours.Key, (StateMachine<TState, TTrigger>.InternalTriggerBehaviour)item, stateReperesentation.UnderlyingState, lookupState));

                    if (item is StateMachine<TState, TTrigger>.DynamicTriggerBehaviour)
                    {
                        var label = $"unknownDestination_{unknowns++}";

                        dynamicTransitions.Add(DynamicTransitionInfo.Create(triggerBehaviours.Key, (StateMachine<TState, TTrigger>.DynamicTriggerBehaviour)item, label));
                    }
                }
            }

            info.SetTransitions(transitions, internalTransitions, dynamicTransitions);
        }

        private StateInfo(
            object underlyingState, 
            Type stateType,
            StateInfo superstate,
            IEnumerable<StateInfo> substates,
            IEnumerable<string> entryActions,
            IEnumerable<string> exitActions,
            IEnumerable<TriggerInfo> ignoredTriggers)
        {
            UnderlyingState = underlyingState;
            StateType = stateType;
            Superstate = superstate;
            Substates = substates ?? throw new ArgumentNullException(nameof(substates));
            EntryActions = entryActions ?? throw new ArgumentNullException(nameof(entryActions));
            ExitActions = exitActions ?? throw new ArgumentNullException(nameof(exitActions));
            IgnoredTriggers = ignoredTriggers ?? throw new ArgumentNullException(nameof(ignoredTriggers));
        }

        private void SetTransitions(IEnumerable<TransitionInfo> transitions,
            IEnumerable<TransitionInfo> internalTransitions,
            IEnumerable<DynamicTransitionInfo> dynamicTransitions)
        {
            Transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
            InternalTransitions = internalTransitions ?? throw new ArgumentNullException(nameof(internalTransitions));
            DynamicTransitions = dynamicTransitions ?? throw new ArgumentNullException(nameof(dynamicTransitions));
        }

        /// <summary>
        /// The instance or value this state represents.
        /// </summary>
        public object UnderlyingState { get; private set; }

        /// <summary>
        /// The type of the underlying state.
        /// </summary>
        /// <returns></returns>
        public Type StateType { get; private set; }

        /// <summary>
        /// Substates defined for this StateResource.
        /// </summary>
        public IEnumerable<StateInfo> Substates { get; private set; }

        /// <summary>
        /// Superstate defined, if any, for this StateResource.
        /// </summary>
        public StateInfo Superstate { get; private set; }

        /// <summary>
        /// Actions that are defined to be exectuted on state-entry.
        /// </summary>
        public IEnumerable<string> EntryActions { get; private set; }

        /// <summary>
        /// Actions that are defined to be exectuted on state-exit.
        /// </summary>
        public IEnumerable<string> ExitActions { get; private set; }

        /// <summary>
        /// Transitions defined for this state.
        /// </summary>
        public IEnumerable<TransitionInfo> Transitions { get; private set; }

        /// <summary>
        /// Transitions defined for this state internally.
        /// </summary>
        public IEnumerable<TransitionInfo> InternalTransitions { get; private set; }

        /// <summary>
        /// Dynamic Transitions defined for this state internally.
        /// </summary>
        public IEnumerable<DynamicTransitionInfo> DynamicTransitions { get; private set; }

        /// <summary>
        /// Triggers ignored for this state.
        /// </summary>
        public IEnumerable<TriggerInfo> IgnoredTriggers { get; private set; }

        /// <summary>
        /// Passes through to the value's ToString.
        /// </summary>
        public override string ToString()
        {
            return UnderlyingState?.ToString() ?? "<null>";
        }
    }
}
