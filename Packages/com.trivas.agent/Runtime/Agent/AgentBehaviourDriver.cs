using System;
using System.Collections.Generic;
using UnityEngine;

namespace TrIVA.Agent
{
    /// <summary>
    /// Turns the agent's conversational state into an animator trigger and a gaze mode, using an Inspector-configured table.
    /// </summary>
    public class AgentBehaviourDriver : MonoBehaviour
    {
        /// <summary>
        /// The animator trigger and gaze mode the agent uses while in one state.
        /// </summary>
        [Serializable]
        private struct StateBehaviour
        {
            public AgentState State;
            public string AnimatorTrigger;
            public GazeMode Gaze;
        }

        [SerializeField] private AgentStateMachine StateMachine;
        [SerializeField] private Animator Animator;
        [SerializeField] private GazeController GazeController;
        [SerializeField] private List<StateBehaviour> Behaviours = new()
        {
            new StateBehaviour { State = AgentState.Inactive, AnimatorTrigger = "Inactive", Gaze = GazeMode.Idle },
            new StateBehaviour { State = AgentState.Idle, AnimatorTrigger = "Idle", Gaze = GazeMode.LookAtUser },
            new StateBehaviour { State = AgentState.Listening, AnimatorTrigger = "Listening", Gaze = GazeMode.LookAtUser },
            new StateBehaviour { State = AgentState.Thinking, AnimatorTrigger = "Thinking", Gaze = GazeMode.Thinking },
            new StateBehaviour { State = AgentState.Speaking, AnimatorTrigger = "Speaking", Gaze = GazeMode.LookAtUser }
        };

        private void OnEnable()
        {
            if (!StateMachine || !Animator || !GazeController)
            {
                Debug.LogError("AgentBehaviourDriver needs an AgentStateMachine, Animator and GazeController assigned.", this);
                enabled = false;
                return;
            }

            StateMachine.StateChanged += OnStateChanged;
            Apply(StateMachine.CurrentState);
        }

        private void OnDisable()
        {
            if (StateMachine)
                StateMachine.StateChanged -= OnStateChanged;
        }

        /// <summary>
        /// Applies the behaviour configured for the newly entered state.
        /// </summary>
        private void OnStateChanged(AgentState previous, AgentState current) => Apply(current);

        /// <summary>
        /// Clears pending state triggers, then fires the state's animator trigger and switches the gaze mode.
        /// </summary>
        private void Apply(AgentState state)
        {
            int index = Behaviours.FindIndex(behaviour => behaviour.State == state);
            if (index < 0)
            {
                Debug.LogWarning($"AgentBehaviourDriver has no behaviour configured for state {state}.", this);
                return;
            }

            foreach (StateBehaviour behaviour in Behaviours)
            {
                if (!string.IsNullOrEmpty(behaviour.AnimatorTrigger))
                    Animator.ResetTrigger(behaviour.AnimatorTrigger);
            }

            StateBehaviour target = Behaviours[index];
            if (!string.IsNullOrEmpty(target.AnimatorTrigger))
                Animator.SetTrigger(target.AnimatorTrigger);

            GazeController.SetMode(target.Gaze);
        }
    }
}
