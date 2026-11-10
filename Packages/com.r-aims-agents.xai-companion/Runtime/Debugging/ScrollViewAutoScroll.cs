using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace XAICompanion.Debugging
{
    /// <summary>
    /// Keeps a ScrollView pinned to its bottom as content is added, by clamping scrollOffset whenever the
    /// content container's resolved size changes.
    /// </summary>
#if UNITY_6000_7_OR_NEWER
    // Callbacks is keyed weakly by ScrollView, so entries from an earlier Play session are collected with their views.
    [Unity.Scripting.LifecycleManagement.NoAutoStaticsCleanup]
#endif
    public static class ScrollViewAutoScroll
    {
        private static readonly ConditionalWeakTable<ScrollView, EventCallback<GeometryChangedEvent>> Callbacks = new();

        /// <summary>
        /// Starts auto-scrolling the given ScrollView to its bottom on every content size change.
        /// </summary>
        public static void Attach(ScrollView scrollView)
        {
            Detach(scrollView);

            EventCallback<GeometryChangedEvent> callback = _ => Pin(scrollView);
            Callbacks.Add(scrollView, callback);
            scrollView.contentContainer.RegisterCallback(callback);
        }

        /// <summary>
        /// Stops auto-scrolling a ScrollView previously attached with Attach.
        /// </summary>
        public static void Detach(ScrollView scrollView)
        {
            if (!Callbacks.TryGetValue(scrollView, out var callback))
                return;

            scrollView.contentContainer.UnregisterCallback(callback);
            Callbacks.Remove(scrollView);
        }

        private static void Pin(ScrollView scrollView)
        {
            scrollView.scrollOffset = new Vector2(scrollView.scrollOffset.x, float.MaxValue);
        }
    }
}
