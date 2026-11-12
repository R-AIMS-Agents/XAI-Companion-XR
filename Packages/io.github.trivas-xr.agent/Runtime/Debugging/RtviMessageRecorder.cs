using TrIVA.Connection;
using UnityEngine;
#if UNITY_EDITOR
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#endif

namespace TrIVA.Debugging
{
    /// <summary>
    /// Records every inbound and outbound RTVI message, unfiltered, to a JSON Lines file under Logs/Rtvi for inspection after a Play session; does nothing outside the Editor.
    /// </summary>
    public class RtviMessageRecorder : MonoBehaviour
    {
        [SerializeField] private LiveKitRtviClient _rtviClient;

#if UNITY_EDITOR
        private const string LogFolder = "Logs/Rtvi";
        private const string InboundDirection = "in";
        private const string OutboundDirection = "out";

        private StreamWriter _writer;

        private void OnEnable()
        {
            string folder = Path.Combine(Path.GetDirectoryName(Application.dataPath), LogFolder);
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, $"rtvi-{DateTime.Now:yyyyMMdd-HHmmss}.jsonl");
            _writer = new StreamWriter(path) { AutoFlush = true };

            _rtviClient.MessageReceived += OnMessageReceived;
            _rtviClient.MessageSent += OnMessageSent;
        }

        private void OnDisable()
        {
            _rtviClient.MessageReceived -= OnMessageReceived;
            _rtviClient.MessageSent -= OnMessageSent;

            _writer?.Dispose();
            _writer = null;
        }

        private void OnMessageReceived(string type, JObject data)
        {
            Write(InboundDirection, type, data);
        }

        private void OnMessageSent(string type, JObject data)
        {
            Write(OutboundDirection, type, data);
        }

        /// <summary>
        /// Appends one message as a single JSON line with timestamp, direction, type and full payload.
        /// </summary>
        private void Write(string direction, string type, JObject data)
        {
            JObject line = new JObject
            {
                ["time"] = DateTime.Now.ToString("O"),
                ["direction"] = direction,
                ["type"] = type,
                ["data"] = data
            };
            _writer.WriteLine(line.ToString(Formatting.None));
        }
#endif
    }
}
