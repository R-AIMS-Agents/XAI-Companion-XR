using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace TrIVA.Debugging
{
    /// <summary>
    /// Timestamped, capped list of rows rendered into a ScrollView, with pause, clear and a per-row filter; shared by the debug panels.
    /// </summary>
    public class CappedLogList
    {
        private const string EntryClass = "dbg-entry";
        private const string TimeClass = "dbg-entry__time";
        private const string MarkerClass = "dbg-entry__marker";
        private const string PrefixClass = "dbg-entry__prefix";
        private const string TextClass = "dbg-entry__text";
        private const string TimeFormat = "HH:mm:ss";

        private readonly List<Entry> _entries = new();
        private ScrollView _scrollView;
        private bool _paused;

        private class Entry
        {
            public string Time;
            public string Prefix;
            public string Text;
            public string ModifierClass;
            public VisualElement Row;
        }

        /// <summary>
        /// Raised after entries are added or cleared, so the owner can refresh counts.
        /// </summary>
        public event Action Changed;

        /// <summary>
        /// Decides from an entry's modifier class whether it is shown; null shows every entry.
        /// </summary>
        public Func<string, bool> Filter { get; set; }

        /// <summary>
        /// Maximum number of buffered entries; the oldest is dropped beyond it.
        /// </summary>
        public int MaxEntries { get; }

        /// <summary>
        /// Number of buffered entries, shown or not.
        /// </summary>
        public int Count => _entries.Count;

        /// <summary>
        /// While paused, entries keep buffering but the view stays frozen; unpausing rebuilds the view.
        /// </summary>
        public bool Paused
        {
            get => _paused;
            set
            {
                if (_paused == value)
                    return;

                _paused = value;
                if (!_paused)
                    Rebuild();
            }
        }

        /// <summary>
        /// Creates an empty list that keeps at most the given number of entries.
        /// </summary>
        public CappedLogList(int maxEntries)
        {
            MaxEntries = maxEntries;
        }

        /// <summary>
        /// Renders the buffer into the given ScrollView and keeps it pinned to the bottom as rows arrive.
        /// </summary>
        public void Bind(ScrollView scrollView)
        {
            Unbind();
            _scrollView = scrollView;
            ScrollViewAutoScroll.Attach(_scrollView);
            Rebuild();
        }

        /// <summary>
        /// Detaches from the ScrollView; entries keep buffering.
        /// </summary>
        public void Unbind()
        {
            if (_scrollView == null)
                return;

            ScrollViewAutoScroll.Detach(_scrollView);
            _scrollView = null;
        }

        /// <summary>
        /// Buffers a timestamped entry, drops the oldest beyond the cap, and shows it unless paused or filtered out.
        /// </summary>
        public void Add(string prefix, string text, string modifierClass)
        {
            Entry entry = new Entry
            {
                Time = DateTime.Now.ToString(TimeFormat),
                Prefix = prefix,
                Text = text,
                ModifierClass = modifierClass
            };
            _entries.Add(entry);

            if (_entries.Count > MaxEntries)
            {
                if (!_paused)
                    _entries[0].Row?.RemoveFromHierarchy();
                _entries.RemoveAt(0);
            }

            if (_scrollView != null && !_paused && Passes(entry))
                AddRow(entry);

            Changed?.Invoke();
        }

        /// <summary>
        /// Drops every buffered entry and empties the view.
        /// </summary>
        public void Clear()
        {
            _entries.Clear();
            _scrollView?.Clear();
            Changed?.Invoke();
        }

        /// <summary>
        /// Counts buffered entries carrying the given modifier class.
        /// </summary>
        public int CountOf(string modifierClass)
        {
            int count = 0;
            foreach (Entry entry in _entries)
            {
                if (entry.ModifierClass == modifierClass)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Counts buffered entries that pass the current filter.
        /// </summary>
        public int CountVisible()
        {
            int count = 0;
            foreach (Entry entry in _entries)
            {
                if (Passes(entry))
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Rebuilds the view from the buffer, applying the current filter.
        /// </summary>
        public void Rebuild()
        {
            if (_scrollView == null)
                return;

            _scrollView.Clear();
            foreach (Entry entry in _entries)
            {
                entry.Row = null;
                if (Passes(entry))
                    AddRow(entry);
            }
        }

        private bool Passes(Entry entry)
        {
            return Filter == null || Filter(entry.ModifierClass);
        }

        /// <summary>
        /// Builds one row: timestamp, level marker, optional prefix and the wrapping text.
        /// </summary>
        private void AddRow(Entry entry)
        {
            VisualElement row = new VisualElement();
            row.AddToClassList(EntryClass);
            if (entry.ModifierClass != null)
                row.AddToClassList(entry.ModifierClass);

            row.Add(NewLabel(entry.Time, TimeClass));

            VisualElement marker = new VisualElement();
            marker.AddToClassList(MarkerClass);
            row.Add(marker);

            if (entry.Prefix != null)
                row.Add(NewLabel(entry.Prefix, PrefixClass));

            row.Add(NewLabel(entry.Text, TextClass));

            entry.Row = row;
            _scrollView.Add(row);
        }

        private static Label NewLabel(string text, string className)
        {
            Label label = new Label(text) { enableRichText = false };
            label.AddToClassList(className);
            return label;
        }
    }
}
