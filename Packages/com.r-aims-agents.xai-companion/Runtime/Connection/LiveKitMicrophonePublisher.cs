using System;
using System.Collections;
using System.Threading;
using LiveKit;
using LiveKit.Proto;
using UnityEngine;
using UnityEngine.InputSystem;

namespace XAICompanion.Connection
{
    /// <summary>
    /// Captures the local microphone and publishes it to the connected LiveKit room, either always on or gated by an
    /// optional push-to-talk input action. Captures from the system default device unless another device is selected.
    /// </summary>
    [RequireComponent(typeof(LiveKitConnector))]
    public class LiveKitMicrophonePublisher : MonoBehaviour
    {
        private const string TrackName = "microphone";
        private const int MaxBitrate = 64000;
        private const string DevicePrefsKey = "Connection.MicrophoneDevice";

        [Header("Push-to-talk")]
        [SerializeField] private bool PushToTalk;
        [SerializeField] private InputActionReference PushToTalkAction;

        [Header("Audio Processing")]
        [SerializeField] private bool EchoCancellation = true;
        [SerializeField] private bool NoiseSuppression = true;
        [SerializeField] private bool AutoGainControl = true;

        private Room _room;
        private GameObject _sourceObject;
        private MicrophoneSource _source;
        private LocalAudioTrack _track;
        private bool _muted;
        private bool _publishing;
        private float _inputLevel;

        /// <summary>
        /// The selected capture device name, or null for the system default device.
        /// </summary>
        public string Device { get; private set; }

        /// <summary>
        /// RMS level (0 to 1) of the latest raw microphone buffer, before audio processing; 0 while no source is live.
        /// </summary>
        public float InputLevel => _source != null ? Volatile.Read(ref _inputLevel) : 0f;

        private void Awake()
        {
            string savedDevice = PlayerPrefs.GetString(DevicePrefsKey, null);
            if (IsAvailable(savedDevice))
                Device = savedDevice;

            GetComponent<LiveKitConnector>().Connected += OnConnected;

            if (PushToTalk && PushToTalkAction != null)
                PushToTalkAction.action.Enable();
        }

        private void Update()
        {
            if (!PushToTalk || _track == null || !PushToTalkAction)
                return;

            bool talking = PushToTalkAction.action.IsPressed();
            if (talking == _muted)
                SetMuted(!talking);
        }

        private void OnDestroy()
        {
            TearDown();
        }

        /// <summary>
        /// Selects the capture device (null for the system default), remembers it across sessions and republishes the track if one is live.
        /// </summary>
        public void SetDevice(string device)
        {
            if (device == Device)
                return;

            Device = device;
            if (device == null)
                PlayerPrefs.DeleteKey(DevicePrefsKey);
            else
                PlayerPrefs.SetString(DevicePrefsKey, device);
            PlayerPrefs.Save();

            if (_room != null && !_publishing)
                StartCoroutine(PublishSelectedDevice(_room));
        }

        /// <summary>
        /// Starts the publish flow once the connector reports a successful connection.
        /// </summary>
        private void OnConnected(Room room)
        {
            _room = room;
            _room.Disconnected += OnDisconnected;
            StartCoroutine(PublishSelectedDevice(room));
        }

        /// <summary>
        /// Releases the microphone when the room disconnects, so the next connection publishes a fresh track.
        /// </summary>
        private void OnDisconnected(Room room)
        {
            TearDown();
        }

        /// <summary>
        /// Publishes the selected device, replacing any live track, and repeats while the selection changed during publishing.
        /// </summary>
        private IEnumerator PublishSelectedDevice(Room room)
        {
            _publishing = true;
            string device;
            do
            {
                device = Device;
                if (_track != null)
                    yield return Unpublish(room);
                yield return Publish(room, device);
            } while (_room == room && device != Device);

            if (_room == room)
                _publishing = false;
        }

        /// <summary>
        /// Unpublishes the live microphone track and releases its source.
        /// </summary>
        private IEnumerator Unpublish(Room room)
        {
            UnpublishTrackInstruction unpublish = room.LocalParticipant.UnpublishTrack(_track, true);
            ReleaseSource();
            yield return unpublish;

            if (unpublish.IsError)
                Debug.LogError("Failed to unpublish microphone track.");
        }

        /// <summary>
        /// Requests microphone permission, creates the processed microphone source for the device (null for system default) and publishes it as the room's microphone track.
        /// </summary>
        private IEnumerator Publish(Room room, string device)
        {
            if (PushToTalk && !PushToTalkAction)
                Debug.LogError("Push-to-talk is enabled but no input action is assigned, microphone will stay muted.");

            yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
            if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
            {
                Debug.LogError("Microphone permission denied, not publishing microphone.");
                yield break;
            }

            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone device found, not publishing microphone.");
                yield break;
            }

            if (!IsAvailable(device))
            {
                Debug.LogWarning($"Microphone '{device}' not found, using system default.");
                device = null;
            }

            string deviceLabel = device ?? "system default";
            GameObject sourceObject = new($"Microphone: {deviceLabel}");
            sourceObject.transform.SetParent(transform, false);

            AudioProcessingOptions processing = new()
            {
                EchoCancellation = EchoCancellation,
                NoiseSuppression = NoiseSuppression,
                AutoGainControl = AutoGainControl
            };
            MicrophoneSource source = new(device, sourceObject, processing);
            LocalAudioTrack track = LocalAudioTrack.CreateAudioTrack(TrackName, source, room);

            TrackPublishOptions options = new()
            {
                AudioEncoding = new AudioEncoding { MaxBitrate = MaxBitrate },
                Source = TrackSource.SourceMicrophone
            };
            PublishTrackInstruction publish = room.LocalParticipant.PublishTrack(track, options);
            yield return publish;

            if (publish.IsError || _room != room)
            {
                Debug.LogError("Failed to publish microphone track.");
                source.Dispose();
                Destroy(sourceObject);
                yield break;
            }

            _sourceObject = sourceObject;
            _source = source;
            _track = track;
            _source.AudioRead += OnAudioRead;
            _source.Start();
            _muted = false;
            if (PushToTalk)
                SetMuted(true);

            Debug.Log($"Microphone '{deviceLabel}' published (push-to-talk: {PushToTalk}, audio processing: {_source.AudioProcessingEnabled})");
        }

        /// <summary>
        /// Mutes or unmutes the published track, which also signals the mute state to the backend.
        /// </summary>
        private void SetMuted(bool muted)
        {
            ((ILocalTrack)_track).SetMute(muted);
            _muted = muted;
        }

        /// <summary>
        /// Stores the RMS of each captured buffer as the input level; runs on the audio thread.
        /// </summary>
        private void OnAudioRead(float[] data, int channels, int sampleRate)
        {
            if (data.Length == 0)
                return;

            float sum = 0f;
            for (int i = 0; i < data.Length; i++)
                sum += data[i] * data[i];

            Volatile.Write(ref _inputLevel, Mathf.Sqrt(sum / data.Length));
        }

        /// <summary>
        /// Returns whether the device is null (system default) or currently listed by Unity.
        /// </summary>
        private static bool IsAvailable(string device)
        {
            return device == null || Array.IndexOf(Microphone.devices, device) >= 0;
        }

        /// <summary>
        /// Drops the room reference and releases the microphone source.
        /// </summary>
        private void TearDown()
        {
            if (_room != null)
            {
                _room.Disconnected -= OnDisconnected;
                _room = null;
            }

            ReleaseSource();
        }

        /// <summary>
        /// Stops and disposes the microphone source, destroys its host object and drops the track reference.
        /// </summary>
        private void ReleaseSource()
        {
            if (_source != null)
            {
                _source.AudioRead -= OnAudioRead;
                _source.Stop();
                _source.Dispose();
                _source = null;
            }

            if (_sourceObject)
            {
                Destroy(_sourceObject);
                _sourceObject = null;
            }

            _track = null;
        }
    }
}
