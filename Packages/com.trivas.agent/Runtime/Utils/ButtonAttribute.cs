using System;

namespace TrIVA.Utils
{
    /// <summary>
    /// Shows a method of a MonoBehaviour as a button at the bottom of its inspector; parameters of simple types get an input field each.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ButtonAttribute : Attribute
    {
        /// <summary>
        /// Button text; the method name in readable form when empty.
        /// </summary>
        public string Label { get; }

        /// <summary>
        /// Enables the button only in Play mode.
        /// </summary>
        public bool PlayModeOnly { get; set; }

        /// <summary>
        /// Marks the method as an inspector button with an optional label.
        /// </summary>
        public ButtonAttribute(string label = null)
        {
            Label = label;
        }
    }
}
