using System.Collections.Generic;
using PurrNet.UI;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    [CustomEditor(typeof(ColorPalette))]
    [CanEditMultipleObjects]
    public class ColorPaletteEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawColors();
            EditorGUILayout.Space();
            DrawStyles();

            serializedObject.ApplyModifiedProperties();

            if (targets.Length != 1)
                return;

            var palette = (ColorPalette)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Colors", EditorStyles.boldLabel);
            DrawSwatches(palette);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(100f, 180f, GUILayout.ExpandWidth(true));
            DrawPreview(rect, palette);
        }

        // --- colors list ------------------------------------------------------

        void DrawColors()
        {
            var colorsProp = serializedObject.FindProperty("_colors");
            if (colorsProp == null)
                return;

            EditorGUILayout.LabelField($"Palette colors ({colorsProp.arraySize})", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Order is the numbering: a ColorInfo stores an index, so append new colors at the end. " +
                "Entries marked hidden stay resolvable but are not offered in pickers.",
                MessageType.None);

            int removeAt = -1;

            for (int i = 0; i < colorsProp.arraySize; i++)
            {
                var entry = colorsProp.GetArrayElementAtIndex(i);
                var nameProp = entry.FindPropertyRelative("name");
                var colorProp = entry.FindPropertyRelative("color");
                var hasContrastProp = entry.FindPropertyRelative("hasContrast");
                var contrastProp = entry.FindPropertyRelative("contrast");
                var hiddenProp = entry.FindPropertyRelative("hidden");

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(i.ToString(), EditorStyles.miniLabel, GUILayout.Width(20f));

                    nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.MinWidth(70f));

                    EditorGUI.BeginChangeCheck();
                    var color = EditorGUILayout.ColorField(GUIContent.none, colorProp.colorValue,
                        false, false, true, GUILayout.Width(48f));
                    if (EditorGUI.EndChangeCheck())
                        colorProp.colorValue = color;

                    hasContrastProp.boolValue = EditorGUILayout.ToggleLeft("on", hasContrastProp.boolValue, GUILayout.Width(34f));

                    using (new EditorGUI.DisabledScope(!hasContrastProp.boolValue))
                    {
                        EditorGUI.BeginChangeCheck();
                        var contrast = EditorGUILayout.ColorField(GUIContent.none, contrastProp.colorValue,
                            false, false, true, GUILayout.Width(48f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            contrastProp.colorValue = contrast;
                            hasContrastProp.boolValue = true;
                        }
                    }

                    hiddenProp.boolValue = EditorGUILayout.ToggleLeft("hidden", hiddenProp.boolValue, GUILayout.Width(58f));

                    if (GUILayout.Button("✕", GUILayout.Width(22f)))
                        removeAt = i;
                }
            }

            if (removeAt >= 0)
            {
                if (removeAt < colorsProp.arraySize - 1 &&
                    !EditorUtility.DisplayDialog("Remove color",
                        $"Removing index {removeAt} shifts every color after it, so existing references " +
                        "to those indices will point at the wrong color. Continue?", "Remove", "Cancel"))
                {
                    removeAt = -1;
                }

                if (removeAt >= 0)
                    colorsProp.DeleteArrayElementAtIndex(removeAt);
            }

            if (GUILayout.Button("Add color"))
            {
                int index = colorsProp.arraySize;
                colorsProp.InsertArrayElementAtIndex(index);
                var entry = colorsProp.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("name").stringValue = "Color " + index;
                entry.FindPropertyRelative("color").colorValue = Color.grey;
                entry.FindPropertyRelative("hasContrast").boolValue = false;
                entry.FindPropertyRelative("contrast").colorValue = Color.white;
                entry.FindPropertyRelative("hidden").boolValue = false;
            }
        }

        void DrawStyles()
        {
            var stylesProp = serializedObject.FindProperty("_styles");
            if (stylesProp != null)
                EditorGUILayout.PropertyField(stylesProp, new GUIContent("Default Styles"), true);
        }

        // --- preview ----------------------------------------------------------

        static void DrawSwatches(ColorPalette palette)
        {
            for (int i = 0; i < palette.count; i++)
            {
                var entry = palette.colors[i];

                var row = GUILayoutUtility.GetRect(0f, 18f, GUILayout.ExpandWidth(true));
                var swatch = new Rect(row.x, row.y + 1f, 34f, row.height - 2f);
                EditorGUI.DrawRect(swatch, entry.color);

                var contrastSwatch = new Rect(swatch.xMax + 3f, row.y + 1f, 18f, row.height - 2f);
                EditorGUI.DrawRect(contrastSwatch, entry.ResolveContrast());

                var labelRect = new Rect(contrastSwatch.xMax + 6f, row.y, row.width, row.height);
                string label = i + "   " + entry.name;
                if (entry.hidden)
                    label += "   (hidden)";
                EditorGUI.LabelField(labelRect, label);
            }
        }

        static void DrawPreview(Rect rect, ColorPalette palette)
        {
            EditorGUI.DrawRect(rect, Role(palette, ColorType.Background, new Color(0.06f, 0.06f, 0.06f, 1f)));

            const float outerPad = 12f;
            var card = new Rect(rect.x + outerPad, rect.y + outerPad,
                                rect.width - outerPad * 2f, rect.height - outerPad * 2f);
            DrawRoundedRect(card, Role(palette, ColorType.Surface, new Color(0.14f, 0.14f, 0.14f, 1f)), palette.styles.panelRoundness);

            const float innerPad = 10f;
            float x = card.x + innerPad;
            float y = card.y + innerPad;
            float contentWidth = card.width - innerPad * 2f;

            var surfaceFg = RoleContrast(palette, ColorType.Surface);
            var muted = Role(palette, ColorType.Muted, surfaceFg * 0.7f);

            DrawBar(x, y, contentWidth * 0.60f, 8f, surfaceFg);
            y += 14f;

            DrawBar(x, y, contentWidth * 0.45f, 5f, muted);
            y += 10f;

            DrawBar(x, y, contentWidth * 0.30f, 5f, surfaceFg);
            y += 18f;

            const float buttonHeight = 26f;
            float buttonWidth = contentWidth * 0.38f;
            var button = new Rect(x, y, buttonWidth, buttonHeight);
            DrawRoundedRect(button, Role(palette, ColorType.Accent, new Color(0.25f, 0.48f, 0.45f, 1f)), palette.styles.buttonRoundness);
            DrawBar(button.x + 10f, button.y + button.height * 0.5f - 3f,
                    button.width - 20f, 6f, RoleContrast(palette, ColorType.Accent));
            y += buttonHeight + 12f;

            const float chipGap = 6f;
            float chipWidth = (contentWidth - chipGap * 2f) / 3f;
            const float chipHeight = 22f;
            DrawChip(new Rect(x, y, chipWidth, chipHeight), Role(palette, ColorType.Success, new Color(0.43f, 0.61f, 0.45f, 1f)),
                RoleContrast(palette, ColorType.Success), palette.styles.buttonRoundness);
            DrawChip(new Rect(x + chipWidth + chipGap, y, chipWidth, chipHeight), Role(palette, ColorType.Warning, new Color(0.73f, 0.56f, 0.29f, 1f)),
                RoleContrast(palette, ColorType.Warning), palette.styles.buttonRoundness);
            DrawChip(new Rect(x + (chipWidth + chipGap) * 2f, y, chipWidth, chipHeight), Role(palette, ColorType.Danger, new Color(0.63f, 0.24f, 0.22f, 1f)),
                RoleContrast(palette, ColorType.Danger), palette.styles.buttonRoundness);
        }

        static Color Role(ColorPalette palette, ColorType type, Color fallback)
        {
            return palette.IsValid((int)type) ? palette.GetColor(type) : fallback;
        }

        static Color RoleContrast(ColorPalette palette, ColorType type)
        {
            if (palette.IsValid((int)type))
                return palette.GetContrast(type);

            return ContrastFallback(Role(palette, type, new Color(0.5f, 0.5f, 0.5f, 1f)));
        }

        static Color ContrastFallback(Color color)
        {
            float luminance = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
            return luminance > 0.5f ? new Color(0.1f, 0.1f, 0.1f, 1f) : Color.white;
        }

        static void DrawBar(float x, float y, float w, float h, Color c)
        {
            EditorGUI.DrawRect(new Rect(x, y, w, h), c);
        }

        static void DrawRoundedRect(Rect rect, Color color, CornerRoundness roundness)
        {
            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill,
                true, 0f, color, 0f, roundness.Resolve(rect.width, rect.height).x);
        }

        static void DrawChip(Rect rect, Color background, Color foreground, CornerRoundness roundness)
        {
            DrawRoundedRect(rect, background, roundness);
            DrawBar(rect.x + 6f, rect.y + rect.height * 0.5f - 2f,
                    rect.width - 12f, 4f, foreground);
        }
    }

    [CustomPropertyDrawer(typeof(CornerRoundness))]
    public class CornerRoundnessDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var content = EditorGUI.PrefixLabel(position, label);
            var full = property.FindPropertyRelative("full");
            var pixels = property.FindPropertyRelative("pixels");
            int previousIndent = EditorGUI.indentLevel;
            bool previousMixed = EditorGUI.showMixedValue;
            EditorGUI.indentLevel = 0;

            var fullRect = new Rect(content.x, content.y, 52f, content.height);
            EditorGUI.showMixedValue = full.hasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            bool fullValue = EditorGUI.ToggleLeft(fullRect, "Full", full.boolValue);
            if (EditorGUI.EndChangeCheck())
                full.boolValue = fullValue;

            var pixelsRect = new Rect(fullRect.xMax + 4f, content.y, Mathf.Max(0f, content.width - 79f), content.height);
            using (new EditorGUI.DisabledScope(full.boolValue && !full.hasMultipleDifferentValues))
            {
                EditorGUI.showMixedValue = pixels.hasMultipleDifferentValues;
                EditorGUI.BeginChangeCheck();
                float value = EditorGUI.FloatField(pixelsRect, pixels.floatValue);
                if (EditorGUI.EndChangeCheck())
                    pixels.floatValue = Mathf.Max(0f, value);
                EditorGUI.LabelField(new Rect(pixelsRect.xMax + 3f, content.y, 20f, content.height), "px");
            }

            EditorGUI.showMixedValue = previousMixed;
            EditorGUI.indentLevel = previousIndent;
            EditorGUI.EndProperty();
        }
    }
}
