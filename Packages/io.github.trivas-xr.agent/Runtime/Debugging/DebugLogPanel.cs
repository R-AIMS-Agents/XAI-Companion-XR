using UnityEngine;
using UnityEngine.UIElements;

namespace TrIVA.Debugging
{
    /// <summary>
    /// Mirrors Unity's Debug.Log/LogWarning/LogError output into the debug panel's Console tab for in-headset
    /// inspection, with timestamps, per-level filter toggles showing counts, and pause and clear controls.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class DebugLogPanel : MonoBehaviour
    {
        [SerializeField] private int _maxLines = 200;

        private const string LogScrollViewName = "log-scroll-view";
        private const string LogToggleName = "log-filter-toggle";
        private const string WarningToggleName = "warning-filter-toggle";
        private const string ErrorToggleName = "error-filter-toggle";
        private const string SummaryLabelName = "log-summary-label";
        private const string PauseButtonName = "log-pause-button";
        private const string ClearButtonName = "log-clear-button";

        private const string LogEntryClass = "dbg-entry--log";
        private const string WarningEntryClass = "dbg-entry--warn";
        private const string ErrorEntryClass = "dbg-entry--error";

        private CappedLogList _log;
        private UIDocument _document;
        private Toggle _logToggle;
        private Toggle _warningToggle;
        private Toggle _errorToggle;
        private Label _summaryLabel;
        private Button _pauseButton;
        private Button _clearButton;

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
            _log = new CappedLogList(_maxLines) { Filter = PassesFilter };
        }

        private void OnEnable()
        {
            Application.logMessageReceived += OnLogMessageReceived;
            _log.Changed += RefreshCounts;
            BindElements(_document.rootVisualElement);
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            _log.Changed -= RefreshCounts;

            _logToggle?.UnregisterValueChangedCallback(OnFilterChanged);
            _warningToggle?.UnregisterValueChangedCallback(OnFilterChanged);
            _errorToggle?.UnregisterValueChangedCallback(OnFilterChanged);
            if (_pauseButton != null)
                _pauseButton.clicked -= OnPauseClicked;
            if (_clearButton != null)
                _clearButton.clicked -= OnClearClicked;
            _log.Unbind();
        }

        /// <summary>
        /// Queries the Console tab's elements from the document's visual tree, then renders any log lines that arrived before the UI was ready.
        /// </summary>
        private void BindElements(VisualElement rootElement)
        {
            _logToggle = rootElement.Q<Toggle>(LogToggleName);
            _warningToggle = rootElement.Q<Toggle>(WarningToggleName);
            _errorToggle = rootElement.Q<Toggle>(ErrorToggleName);
            _summaryLabel = rootElement.Q<Label>(SummaryLabelName);
            _pauseButton = rootElement.Q<Button>(PauseButtonName);
            _clearButton = rootElement.Q<Button>(ClearButtonName);

            _logToggle.RegisterValueChangedCallback(OnFilterChanged);
            _warningToggle.RegisterValueChangedCallback(OnFilterChanged);
            _errorToggle.RegisterValueChangedCallback(OnFilterChanged);
            _pauseButton.clicked += OnPauseClicked;
            _clearButton.clicked += OnClearClicked;
            _pauseButton.text = _log.Paused ? "Resume" : "Pause";

            _log.Bind(rootElement.Q<ScrollView>(LogScrollViewName));
            RefreshCounts();
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            _log.Add(null, condition, EntryClassForType(type));
        }

        private void OnFilterChanged(ChangeEvent<bool> _)
        {
            _log.Rebuild();
            RefreshCounts();
        }

        private void OnPauseClicked()
        {
            _log.Paused = !_log.Paused;
            _pauseButton.text = _log.Paused ? "Resume" : "Pause";
            RefreshCounts();
        }

        private void OnClearClicked()
        {
            _log.Clear();
        }

        /// <summary>
        /// Writes per-level counts onto the filter toggles and the visible/buffered totals into the footer.
        /// </summary>
        private void RefreshCounts()
        {
            if (_summaryLabel == null)
                return;

            _logToggle.text = $"Log {_log.CountOf(LogEntryClass)}";
            _warningToggle.text = $"Warning {_log.CountOf(WarningEntryClass)}";
            _errorToggle.text = $"Error {_log.CountOf(ErrorEntryClass)}";

            string paused = _log.Paused ? "Paused · " : "";
            _summaryLabel.text = $"{paused}{_log.CountVisible()} of {_log.Count} lines · buffer {_log.MaxEntries}";
        }

        private bool PassesFilter(string entryClass)
        {
            switch (entryClass)
            {
                case WarningEntryClass:
                    return _warningToggle == null || _warningToggle.value;
                case ErrorEntryClass:
                    return _errorToggle == null || _errorToggle.value;
                default:
                    return _logToggle == null || _logToggle.value;
            }
        }

        private static string EntryClassForType(LogType type)
        {
            switch (type)
            {
                case LogType.Warning:
                    return WarningEntryClass;
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    return ErrorEntryClass;
                default:
                    return LogEntryClass;
            }
        }
    }
}
