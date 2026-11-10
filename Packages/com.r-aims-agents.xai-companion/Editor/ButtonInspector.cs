using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace XAICompanion.Utils.Editor
{
    /// <summary>
    /// Default inspector for every MonoBehaviour that adds a button for each method marked with <see cref="ButtonAttribute"/>.
    /// </summary>
    [CustomEditor(typeof(MonoBehaviour), true)]
    [CanEditMultipleObjects]
    public class ButtonInspector : UnityEditor.Editor
    {
        private const BindingFlags MethodFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>
        /// Builds the default inspector and appends the method buttons below it.
        /// </summary>
        public override VisualElement CreateInspectorGUI()
        {
            VisualElement root = new();
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            Label header = new("Debug Buttons");
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginTop = 12;
            header.style.marginLeft = 3;

            foreach (MethodInfo method in target.GetType().GetMethods(MethodFlags))
            {
                ButtonAttribute attribute = method.GetCustomAttribute<ButtonAttribute>();
                if (attribute == null)
                    continue;
                if (header.parent == null)
                    root.Add(header);
                root.Add(CreateMethodButton(method, attribute));
            }

            return root;
        }

        /// <summary>
        /// Creates the button for one method; a method with parameters gets a framed box holding an input field per parameter above its button.
        /// </summary>
        private VisualElement CreateMethodButton(MethodInfo method, ButtonAttribute attribute)
        {
            ParameterInfo[] parameters = method.GetParameters();
            VisualElement container = parameters.Length > 0 ? new Box() : new VisualElement();
            container.style.marginTop = 4;
            if (parameters.Length > 0)
            {
                container.style.paddingTop = container.style.paddingBottom = 4;
                container.style.paddingLeft = container.style.paddingRight = 4;
            }

            object[] arguments = new object[parameters.Length];
            bool supported = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                VisualElement field = CreateParameterField(parameters[i], arguments, i);
                if (field == null)
                {
                    supported = false;
                    break;
                }
                container.Add(field);
            }

            string label = string.IsNullOrEmpty(attribute.Label) ? ObjectNames.NicifyVariableName(method.Name) : attribute.Label;
            Button button = new(() => Invoke(method, arguments, label)) { text = label };
            container.Add(button);

            if (!supported)
            {
                button.SetEnabled(false);
                button.tooltip = "A parameter type has no input field.";
            }
            else if (attribute.PlayModeOnly)
            {
                button.tooltip = "Available in Play mode.";
                button.SetEnabled(EditorApplication.isPlaying);
                button.schedule.Execute(() => button.SetEnabled(EditorApplication.isPlaying)).Every(250);
            }

            return container;
        }

        /// <summary>
        /// Creates an input field that writes the parameter's value into the argument array, or returns null for unsupported types.
        /// </summary>
        private static VisualElement CreateParameterField(ParameterInfo parameter, object[] arguments, int index)
        {
            Type type = parameter.ParameterType;
            string label = ObjectNames.NicifyVariableName(parameter.Name);
            arguments[index] = parameter.HasDefaultValue ? parameter.DefaultValue : type.IsValueType ? Activator.CreateInstance(type) : null;

            return type switch
            {
                _ when type == typeof(int) => Bind(new IntegerField(label), arguments, index),
                _ when type == typeof(float) => Bind(new FloatField(label), arguments, index),
                _ when type == typeof(bool) => Bind(new Toggle(label), arguments, index),
                _ when type == typeof(string) => Bind(new TextField(label), arguments, index),
                _ when type == typeof(Vector2) => Bind(new Vector2Field(label), arguments, index),
                _ when type == typeof(Vector3) => Bind(new Vector3Field(label), arguments, index),
                _ when type == typeof(Color) => Bind(new ColorField(label), arguments, index),
                _ when typeof(Object).IsAssignableFrom(type) => Bind(new ObjectField(label) { objectType = type, allowSceneObjects = true }, arguments, index),
                _ => null
            };
        }

        /// <summary>
        /// Sets the field to the current argument value and keeps the argument in sync with later edits.
        /// </summary>
        private static VisualElement Bind<T>(BaseField<T> field, object[] arguments, int index)
        {
            if (arguments[index] is T value)
                field.SetValueWithoutNotify(value);
            field.RegisterValueChangedCallback(evt => arguments[index] = evt.newValue);
            return field;
        }

        /// <summary>
        /// Calls the method on every selected object, recording an undo step when outside Play mode.
        /// </summary>
        private void Invoke(MethodInfo method, object[] arguments, string label)
        {
            foreach (Object selected in targets)
            {
                if (!EditorApplication.isPlaying)
                    Undo.RecordObject(selected, label);
                method.Invoke(method.IsStatic ? null : selected, arguments);
            }
        }
    }
}
