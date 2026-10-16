using System.Collections.Generic;
using PurrNet.UI;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    [CustomPropertyDrawer(typeof(ColorInfo))]
    public class ColorInfoDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                var rect = EditorGUI.PrefixLabel(position, label);

                var enabledProp = property.FindPropertyRelative("enabled");
                var colorProp = property.FindPropertyRelative("color");
                var contrastProp = property.FindPropertyRelative("contrast");

                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                const float enableWidth = 16f;
                const float toggleWidth = 80f;
                const float spacing = 4f;

                var enableRect = new Rect(rect.x, rect.y, enableWidth, rect.height);
                var dropdownRect = new Rect(
                    enableRect.xMax + spacing, rect.y,
                    rect.width - enableWidth - toggleWidth - spacing * 2f, rect.height);
                var toggleRect = new Rect(rect.xMax - toggleWidth, rect.y, toggleWidth, rect.height);

                EditorGUI.PropertyField(enableRect, enabledProp, GUIContent.none);

                bool slotActive = !enabledProp.hasMultipleDifferentValues && enabledProp.boolValue;
                using (new EditorGUI.DisabledScope(!slotActive))
                {
                    DrawColorPopup(dropdownRect, colorProp);

                    EditorGUI.BeginChangeCheck();
                    EditorGUI.showMixedValue = contrastProp.hasMultipleDifferentValues;
                    bool newContrast = EditorGUI.ToggleLeft(toggleRect, "Contrast", contrastProp.boolValue);
                    EditorGUI.showMixedValue = false;
                    if (EditorGUI.EndChangeCheck())
                        contrastProp.boolValue = newContrast;
                }

                EditorGUI.indentLevel = previousIndent;
            }
        }

        /// <summary>
        /// Shows the active palette's color names (numbered) instead of the ColorType names, so the
        /// picker reflects what the project actually defines. Falls back to the enum names when no
        /// palette is available.
        /// </summary>
        static void DrawColorPopup(Rect rect, SerializedProperty colorProp)
        {
            if (colorProp.hasMultipleDifferentValues)
            {
                EditorGUI.showMixedValue = true;
                EditorGUI.Popup(rect, 0, new[] { "—" });
                EditorGUI.showMixedValue = false;
                return;
            }

            var settings = PurrUIPaletteSettings.global;
            var palette = settings != null ? settings.palette : null;

            int current = colorProp.intValue;

            var names = new List<string>();
            var indices = new List<int>();

            if (palette != null && palette.count > 0)
            {
                for (int i = 0; i < palette.count; i++)
                {
                    var entry = palette.colors[i];
                    if (entry.hidden && entry.id != current)
                        continue;

                    names.Add(entry.id + "   " + entry.name + (entry.hidden ? "   (hidden)" : string.Empty));
                    indices.Add(entry.id);
                }
            }
            else
            {
                foreach (ColorType value in System.Enum.GetValues(typeof(ColorType)))
                {
                    names.Add((int)value + "   " + value);
                    indices.Add((int)value);
                }
            }

            int selected = indices.IndexOf(current);
            if (selected < 0)
            {
                names.Insert(0, current + "   (not in palette)");
                indices.Insert(0, current);
                selected = 0;
            }

            EditorGUI.BeginChangeCheck();
            int picked = EditorGUI.Popup(rect, selected, names.ToArray());
            if (EditorGUI.EndChangeCheck() && picked >= 0 && picked < indices.Count)
                colorProp.intValue = indices[picked];
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
