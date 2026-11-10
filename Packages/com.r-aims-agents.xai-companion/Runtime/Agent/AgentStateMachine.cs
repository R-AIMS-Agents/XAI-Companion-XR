using System;
using XAICompanion.Connection;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace XAICompanion.Agent
{
    /// <summary>
    /// The conversational state the agent is in, which its animation and gaze follow.
    /// </summary>
    public enum AgentState { Inactive, Idle, Listening, Thinking, Speaking }

    /// <summary>
    /// Derives the agent's conversational state from RTVI events and timeouts and announces every change.
    /// </summary>
    public class AgentStateMachine : MonoBehaviour
    {
        [SerializeField] private LiveKitRtviClient RtviClient;
        [SerializeField, Tooltip("Seconds in Thinking without the bot starting to speak before falling back to Idle.")] private float ThinkingTimeout = 10f;
        [SerializeField, Tooltip("Seconds in Idle before the agent becomes Inactive.")] private float InactiveTimeout = 30f;

        private float _stateEnteredTime;

        /// <summary>
        /// The state the agent is currently in.
        /// </summary>
        public AgentState CurrentState { get; private set; } = AgentState.Idle;

        /// <summary>
        /// Raised on every state change, passing the previous and the new state.
        /// </summary>
        public event Action<AgentState, AgentState> StateChanged;

        private void OnEnable()
        {
            if (!RtviClient)
            {
                Debug.LogError("AgentStateMachine has no LiveKitRtviClient assigned.", this);
                enabled = false;
                return;
            }

            RtviClient.MessageReceived += OnMessageReceived;
            _stateEnteredTime = Time.time;
        }

        private void OnDisable()
        {
            if (RtviClient)
                RtviClient.MessageReceived -= OnMessageReceived;
        }

        private void Update()
        {
            float elapsed = Time.time - _stateEnteredTime;

            if (CurrentState == AgentState.Thinking && elapsed >= ThinkingTimeout)
                SetState(AgentState.Idle);
            else if (CurrentState == AgentState.Idle && elapsed >= InactiveTimeout)
                SetState(AgentState.Inactive);
        }

        /// <summary>
        /// Applies the state transition, if any, that an inbound RTVI message triggers.
        /// </summary>
        private void OnMessageReceived(string type, JObject data)
        {
            switch (type)
            {
                case "user-started-speaking":
                    SetState(AgentState.Listening);
                    break;

                case "user-transcription":
                    bool isFinal = (bool?)data?["final"] == true;
                    if (isFinal && CurrentState == AgentState.Listening)
                        SetState(AgentState.Thinking);
                    else if (!isFinal && CurrentState == AgentState.Thinking)
                        SetState(AgentState.Listening);
                    break;

                case "user-stopped-speaking":
                    if (CurrentState == AgentState.Listening)
                        SetState(AgentState.Thinking);
                    break;

                case "bot-llm-started":
                    SetState(AgentState.Thinking);
                    break;

                case "bot-started-speaking":
                    SetState(AgentState.Speaking);
                    break;

                case "bot-stopped-speaking":
                    if (CurrentState == AgentState.Speaking)
                        SetState(AgentState.Idle);
                    break;
            }
        }

        /// <summary>
        /// Enters a new state, restarts its timeout and raises StateChanged; does nothing if the state is unchanged.
        /// </summary>
        private void SetState(AgentState state)
        {
            if (state == CurrentState)
                return;

            AgentState previous = CurrentState;
            CurrentState = state;
            _stateEnteredTime = Time.time;
            StateChanged?.Invoke(previous, state);
        }
    }
}
