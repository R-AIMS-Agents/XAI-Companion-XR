using System;
using LiveKit;
using UnityEngine;

namespace XAICompanion.Connection
{
    /// <summary>
    /// Plays back the connected LiveKit room's remote audio track through an Inspector-assigned AudioSource.
    /// </summary>
    [RequireComponent(typeof(LiveKitConnector))]
    public class LiveKitAudioPlayback : MonoBehaviour
    {
        [SerializeField] private AudioSource AgentAudioSource;

        /// <summary>
        /// Raised once the remote audio stream is attached, passing the AudioSource it plays on; components added to that GameObject from here on see the stream's samples in their audio filters.
        /// </summary>
        public event Action<AudioSource> AudioStreamStarted;

        /// <summary>
        /// Raised when the remote audio stream is detached from the AudioSource.
        /// </summary>
        public event Action<AudioSource> AudioStreamStopped;

        private Room _room;
        private AudioStream _audioStream;

        private void Awake()
        {
            GetComponent<LiveKitConnector>().Connected += OnConnected;
        }

        private void OnDestroy()
        {
            if (_room != null)
            {
                _room.TrackSubscribed -= OnTrackSubscribed;
                _room.TrackUnsubscribed -= OnTrackUnsubscribed;
            }

            _audioStream?.Dispose();
        }

        /// <summary>
        /// Subscribes to the room's track events once the connector reports a successful connection.
        /// </summary>
        private void OnConnected(Room room)
        {
            _room = room;
            _room.TrackSubscribed += OnTrackSubscribed;
            _room.TrackUnsubscribed += OnTrackUnsubscribed;
        }

        /// <summary>
        /// Starts playback of a newly subscribed remote audio track on the assigned AudioSource.
        /// </summary>
        private void OnTrackSubscribed(IRemoteTrack track, RemoteTrackPublication publication, RemoteParticipant participant)
        {
            if (track is not RemoteAudioTrack audioTrack)
                return;

            if (!AgentAudioSource)
            {
                Debug.LogError("LiveKitAudioPlayback has no AudioSource assigned.");
                return;
            }

            _audioStream = new AudioStream(audioTrack, AgentAudioSource);
            AudioStreamStarted?.Invoke(AgentAudioSource);
        }

        /// <summary>
        /// Stops playback when the corresponding remote audio track is unsubscribed.
        /// </summary>
        private void OnTrackUnsubscribed(IRemoteTrack track, RemoteTrackPublication publication, RemoteParticipant participant)
        {
            if (track is not RemoteAudioTrack)
                return;

            if (_audioStream == null)
                return;

            _audioStream.Dispose();
            _audioStream = null;
            AudioStreamStopped?.Invoke(AgentAudioSource);
        }
    }
}
