using System;
using System.Collections.Generic;
using System.Text;
using TrIVA.Connection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace TrIVA.Tests
{
    /// <summary>
    /// Plays the backend's side of the RTVI data channel for a LiveKitRtviClient: receives and checks every packet the client publishes and sends packets to it as the bot would, without a LiveKit room.
    /// </summary>
    public class FakeRtviBackend
    {
        private const string RtviLabel = "rtvi-ai";
        private const string ClientMessageType = "client-message";
        private const string GameEventType = "game-event";

        private readonly LiveKitRtviClient _client;
        private readonly List<JObject> _messages = new List<JObject>();

        /// <summary>
        /// Creates a backend for the client; nothing passes between them until it connects.
        /// </summary>
        public FakeRtviBackend(LiveKitRtviClient client)
        {
            _client = client;
        }

        /// <summary>
        /// Opens the client's transport to this backend, which makes the client send its client-ready handshake.
        /// </summary>
        public void Connect()
        {
            _client.Open(OnPacket);
        }

        /// <summary>
        /// Closes the client's transport as a dropped LiveKit connection would.
        /// </summary>
        public void Disconnect()
        {
            _client.Close();
        }

        /// <summary>
        /// Tells the client the bot is ready, as the backend does once its pipeline runs.
        /// </summary>
        public void SendBotReady()
        {
            Send("bot-ready", new JObject { ["version"] = "2.1.0", ["about"] = new JObject { ["library"] = "fake-rtvi-backend" } });
        }

        /// <summary>
        /// Returns every RTVI message the client published since the last call, in order, and forgets them.
        /// </summary>
        public JObject[] TakeMessages()
        {
            JObject[] messages = _messages.ToArray();
            _messages.Clear();
            return messages;
        }

        /// <summary>
        /// Returns the game-event payloads the client published since the last call, in order, and forgets them; fails if anything else was published.
        /// </summary>
        public JObject[] TakeGameEvents()
        {
            List<JObject> events = new List<JObject>();
            foreach (JObject message in TakeMessages())
            {
                Assert.AreEqual(ClientMessageType, (string)message["type"], $"Expected only client messages, got {message}");
                JObject data = message["data"] as JObject;
                Assert.IsNotNull(data, $"Client message without data: {message}");
                Assert.AreEqual(GameEventType, (string)data["t"], $"Expected only game events, got {message}");
                JObject payload = data["d"] as JObject;
                Assert.IsNotNull(payload, $"Game event without payload: {message}");
                events.Add(payload);
            }
            return events.ToArray();
        }

        /// <summary>
        /// Encodes an RTVI message as the backend would and hands it to the client as if it arrived over LiveKit.
        /// </summary>
        private void Send(string type, JObject data)
        {
            JObject message = new JObject
            {
                ["id"] = Guid.NewGuid().ToString("N").Substring(0, 8),
                ["label"] = RtviLabel,
                ["type"] = type,
                ["data"] = data
            };
            _client.Receive(Encoding.UTF8.GetBytes(message.ToString(Formatting.None)));
        }

        /// <summary>
        /// Parses a packet the client published as the backend would and checks it is an RTVI message with an id and a type.
        /// </summary>
        private void OnPacket(byte[] bytes)
        {
            JObject message = JObject.Parse(Encoding.UTF8.GetString(bytes));
            Assert.AreEqual(RtviLabel, (string)message["label"], $"Packet is not an RTVI message: {message}");
            Assert.IsFalse(string.IsNullOrEmpty((string)message["id"]), $"RTVI message without id: {message}");
            Assert.IsFalse(string.IsNullOrEmpty((string)message["type"]), $"RTVI message without type: {message}");
            _messages.Add(message);
        }
    }
}
