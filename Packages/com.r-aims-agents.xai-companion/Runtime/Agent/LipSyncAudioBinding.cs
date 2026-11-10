using XAICompanion.Connection;
using uLipSync;
using UnityEngine;

namespace XAICompanion.Agent
{
    /// <summary>
    /// Feeds the agent's LiveKit audio stream into uLipSync by attaching a uLipSyncAudioSource tap once the stream is running, so the tap sits after LiveKit's audio filter and sees the received samples.
    /// </summary>
    public class LipSyncAudioBinding : MonoBehaviour
    {
        [SerializeField] private LiveKitAudioPlayback AudioPlayback;
        [SerializeField] private uLipSync.uLipSync LipSync;

        private uLipSyncAudioSource _tap;

        private void Awake()
        {
            if (!AudioPlayback || !LipSync)
            {
                Debug.LogError("LipSyncAudioBinding needs a LiveKitAudioPlayback and a uLipSync assigned. Lip sync disabled.", this);
                enabled = false;
                return;
            }

            AudioPlayback.AudioStreamStarted += OnAudioStreamStarted;
            AudioPlayback.AudioStreamStopped += OnAudioStreamStopped;
        }

        private void OnDestroy()
        {
            if (!AudioPlayback)
                return;

            AudioPlayback.AudioStreamStarted -= OnAudioStreamStarted;
            AudioPlayback.AudioStreamStopped -= OnAudioStreamStopped;
        }

        /// <summary>
        /// Adds the uLipSync tap to the stream's AudioSource and points uLipSync at it.
        /// </summary>
        private void OnAudioStreamStarted(AudioSource source)
        {
            RemoveTap();
            _tap = source.gameObject.AddComponent<uLipSyncAudioSource>();
            LipSync.audioSourceProxy = _tap;
        }

        /// <summary>
        /// Detaches uLipSync from the stream and removes the tap.
        /// </summary>
        private void OnAudioStreamStopped(AudioSource source)
        {
            RemoveTap();
        }

        /// <summary>
        /// Clears the uLipSync proxy and destroys the current tap, if any.
        /// </summary>
        private void RemoveTap()
        {
            if (!_tap)
                return;

            if (LipSync.audioSourceProxy == _tap)
                LipSync.audioSourceProxy = null;

            Destroy(_tap);
            _tap = null;
        }
    }
}
