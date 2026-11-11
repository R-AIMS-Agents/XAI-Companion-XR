using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using LiveKit;
using LiveKit.Proto;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

[assembly: InternalsVisibleTo("TrIVA.Tests")]

namespace TrIVA.Connection
{
    /// <summary>
    /// Implements the client side of the RTVI protocol over the LiveKit data channel: sends the client-ready
    /// handshake on connect and gives other components a shared Send/MessageReceived surface for further
    /// RTVI message types (tool calls, behaviour streams, game events).
    /// </summary>
    [RequireComponent(typeof(LiveKitConnector))]
#if UNITY_6000_7_OR_NEWER
    // The only static, KnownTypes, is a fixed lookup table that never changes.
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    public class LiveKitRtviClient : MonoBehaviour
    {
        private const string RtviLabel = "rtvi-ai";
        private const string RtviProtocolVersion = "2.1.0";
        private const string ClientLibraryName = "triva-unity";
        private const string ErrorType = "error";

        // RTVI message types consumed elsewhere in the app, or otherwise irrelevant (animation/lip sync are
        // audio-driven, not event-driven); anything else gets an unknown-type debug log.
        private static readonly HashSet<string> KnownTypes = new()
        {
            "bot-output",
            "bot-ready",
            "bot-started-speaking",
            "bot-stopped-speaking",
            "bot-tts-started",
            "bot-tts-stopped",
            "bot-tts-text",
            "bot-llm-started",
            "bot-llm-text",
            "bot-transcription",
            "user-mute-started",
            "user-mute-stopped",
            "user-started-speaking",
            "user-stopped-speaking",
            "user-transcription",
            "user-llm-text",
            "metrics"
        };

        /// <summary>
        /// Raised for every well-formed inbound RTVI message, passing its type and data payload to listeners.
        /// </summary>
        public event Action<string, JObject> MessageReceived;

        /// <summary>
        /// Raised for every outbound RTVI message as it is handed to the data channel, passing its type and data payload.
        /// </summary>
        public event Action<string, JObject> MessageSent;

        /// <summary>
        /// Raised when the transport closes, as when the LiveKit room disconnects; nothing can be sent until it opens again.
        /// </summary>
        public event Action Disconnected;

        private Room _room;
        private Action<byte[]> _publish;

        private void Awake()
        {
            LiveKitConnector connector = GetComponent<LiveKitConnector>();
            connector.Connected += OnConnected;
            connector.Disconnected += Close;
        }

        private void OnDestroy()
        {
            if (_room != null)
                _room.DataReceived -= OnDataReceived;
        }

        /// <summary>
        /// Registers the data handler and opens the client on the room once it is connected.
        /// </summary>
        private void OnConnected(Room room)
        {
            _room = room;
            _room.DataReceived += OnDataReceived;
            Open(PublishToRoom);
        }

        /// <summary>
        /// Sends every outgoing packet to the given publisher from now on and sends the client-ready handshake; the LiveKit room in the app, a fake backend in tests.
        /// </summary>
        internal void Open(Action<byte[]> publish)
        {
            _publish = publish;

            JObject data = new JObject
            {
                ["version"] = RtviProtocolVersion,
                ["about"] = new JObject
                {
                    ["library"] = ClientLibraryName,
                    ["platform"] = "Unity",
                    ["platform_version"] = Application.unityVersion
                }
            };
            Send("client-ready", data);
        }

        /// <summary>
        /// Stops sending and drops the room, if any, and tells listeners the transport closed.
        /// </summary>
        internal void Close()
        {
            if (_room != null)
                _room.DataReceived -= OnDataReceived;
            _room = null;
            _publish = null;
            Disconnected?.Invoke();
        }

        /// <summary>
        /// Encodes an RTVI message of the given type and publishes it over the open transport.
        /// </summary>
        public void Send(string type, JObject data = null)
        {
            if (_publish == null)
            {
                Debug.LogError("Cannot send RTVI message, the transport is not open.");
                return;
            }

            JObject message = new JObject
            {
                ["id"] = Guid.NewGuid().ToString("N").Substring(0, 8),
                ["label"] = RtviLabel,
                ["type"] = type,
                ["data"] = data
            };

            byte[] bytes = Encoding.UTF8.GetBytes(message.ToString(Formatting.None));
            _publish(bytes);
            MessageSent?.Invoke(type, data);
        }

        /// <summary>
        /// Publishes the encoded payload to the connected room.
        /// </summary>
        private void PublishToRoom(byte[] bytes)
        {
            StartCoroutine(PublishData(_room, bytes));
        }

        /// <summary>
        /// Publishes the encoded payload and logs a warning if the server rejects it.
        /// </summary>
        private IEnumerator PublishData(Room room, byte[] bytes)
        {
            PublishDataInstruction instruction = room.LocalParticipant.PublishData(bytes, reliable: true);
            yield return instruction;

            if (instruction.IsError)
                Debug.LogWarning($"Failed to publish RTVI message: {instruction.Error}");
        }

        /// <summary>
        /// Passes a data packet from the room on to be decoded.
        /// </summary>
        private void OnDataReceived(byte[] data, Participant participant, DataPacketKind kind, string topic)
        {
            Receive(data);
        }

        /// <summary>
        /// Decodes an incoming packet and raises MessageReceived for well-formed RTVI messages; packets arrive from the LiveKit room in the app, from a fake backend in tests.
        /// </summary>
        internal void Receive(byte[] data)
        {
            JObject message;
            try
            {
                message = JObject.Parse(Encoding.UTF8.GetString(data));
            }
            catch (JsonException)
            {
                return;
            }

            if ((string)message["label"] != RtviLabel)
                return;

            string type = (string)message["type"];
            if (string.IsNullOrEmpty(type))
                return;

            MessageReceived?.Invoke(type, message["data"] as JObject);

            if (type == ErrorType)
                LogServerError(message["data"] as JObject);
            else if (!KnownTypes.Contains(type))
                Debug.Log($"Unknown RTVI message type '{type}': {message["data"]}");
        }

        /// <summary>
        /// Logs a server-reported RTVI error as an error when fatal, otherwise as a warning.
        /// </summary>
        private static void LogServerError(JObject data)
        {
            string error = (string)data?["error"] ?? "(no error text)";
            if ((bool?)data?["fatal"] == true)
                Debug.LogError($"RTVI fatal error: {error}");
            else
                Debug.LogWarning($"RTVI error: {error}");
        }
    }
}
