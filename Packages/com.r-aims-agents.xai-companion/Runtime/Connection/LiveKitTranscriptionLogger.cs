using Newtonsoft.Json.Linq;
using UnityEngine;

namespace XAICompanion.Connection
{
    /// <summary>
    /// Logs the agent's spoken lines to the console as each RTVI bot-output sentence first appears.
    /// </summary>
    [RequireComponent(typeof(LiveKitRtviClient))]
    public class LiveKitTranscriptionLogger : MonoBehaviour
    {
        private const string BotOutputType = "bot-output";
        private const string NewSpokenStatus = "new";

        private LiveKitRtviClient _rtviClient;

        private void Awake()
        {
            _rtviClient = GetComponent<LiveKitRtviClient>();
            _rtviClient.MessageReceived += OnMessageReceived;
        }

        private void OnDestroy()
        {
            _rtviClient.MessageReceived -= OnMessageReceived;
        }

        /// <summary>
        /// Logs a bot-output sentence's text the first time it appears, ignoring later progress updates.
        /// </summary>
        private void OnMessageReceived(string type, JObject data)
        {
            if (type != BotOutputType || data == null)
                return;

            if ((string)data["spoken_status"] != NewSpokenStatus)
                return;

            string text = (string)data["text"];
            if (!string.IsNullOrEmpty(text))
                Debug.Log($"[Agent] {text}");
        }
    }
}
