using System;
using System.Collections;
using LiveKit;
using UnityEngine;

namespace XAICompanion.Connection
{
    /// <summary>
    /// Fetches LiveKit connection details from the sibling TokenSourceComponent and connects to the room,
    /// retrying on a fixed interval until connected and again whenever the room disconnects.
    /// </summary>
    [RequireComponent(typeof(TokenSourceComponent))]
    public class LiveKitConnector : MonoBehaviour
    {
        private const float RetryIntervalSeconds = 3f;

        /// <summary>
        /// Raised once the room connection succeeds, passing the connected Room to listeners.
        /// </summary>
        public event Action<Room> Connected;

        /// <summary>
        /// Raised whenever the room disconnects, whether from a manual Disconnect() call or a dropped connection.
        /// </summary>
        public event Action Disconnected;

        /// <summary>
        /// Whether a LiveKit room is currently connected.
        /// </summary>
        public bool IsConnected => _room != null;

        private TokenSourceComponent _tokenSource;
        private Room _room;

        private void Awake()
        {
            _tokenSource = GetComponent<TokenSourceComponent>();
        }

        private void Start()
        {
            StartCoroutine(ConnectWithRetry());
        }

        /// <summary>
        /// Connects, then waits for a disconnect to start the retry cycle over, forever.
        /// </summary>
        private IEnumerator ConnectWithRetry()
        {
            while (true)
            {
                yield return Connect();
                yield return new WaitUntil(() => _room == null);
            }
        }

        /// <summary>
        /// Fetches connection details from the token source and connects to the resulting LiveKit room,
        /// retrying on a fixed interval until it succeeds.
        /// </summary>
        private IEnumerator Connect()
        {
            while (true)
            {
                var fetch = _tokenSource.FetchConnectionDetails();
                yield return fetch;

                if (fetch.IsError)
                {
                    Debug.LogWarning($"Failed to fetch LiveKit connection details: {fetch.Exception?.Message}, retrying in {RetryIntervalSeconds}s");
                    yield return new WaitForSeconds(RetryIntervalSeconds);
                    continue;
                }

                Room room = new Room();
                ConnectInstruction connect = room.Connect(fetch.Result.ServerUrl, fetch.Result.ParticipantToken, new RoomOptions());
                yield return connect;

                if (connect.IsError)
                {
                    Debug.LogError($"Failed to connect to LiveKit room, retrying in {RetryIntervalSeconds}s");
                    yield return new WaitForSeconds(RetryIntervalSeconds);
                    continue;
                }

                _room = room;
                _room.Disconnected += OnDisconnected;
                Debug.Log("Connected to LiveKit room");
                Connected?.Invoke(_room);
                yield break;
            }
        }

        /// <summary>
        /// Clears the room reference on disconnect so the retry loop starts a fresh connection attempt.
        /// </summary>
        private void OnDisconnected(Room room)
        {
            room.Disconnected -= OnDisconnected;
            _room = null;
            Debug.LogWarning("Disconnected from LiveKit room, retrying");
            Disconnected?.Invoke();
        }

        /// <summary>
        /// Manually disconnects the current room, if any; the retry loop then reconnects automatically.
        /// </summary>
        public void Disconnect()
        {
            _room?.Disconnect();
        }
    }
}
