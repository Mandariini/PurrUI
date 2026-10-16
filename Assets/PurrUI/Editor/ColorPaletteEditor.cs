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
        // Conventional ids for the roles this project added on top of ColorType.
        const int ProjectRoleOverlay = 9;
        const int ProjectRoleShadow = 10;
        const int ProjectRoleBorder = 11;
        const int ProjectRoleDamage = 12;

        static ColorUsageScanner.Result _usage;
        static ColorPalette _usageFor;
        int _expandedColor = -1;

        void EnsureUsageScan(ColorPalette palette)
        {
            if (_usage != null && _usageFor == palette)
                return;

            _usage = ColorUsageScanner.Scan(palette);
            _usageFor = palette;
        }

        public override void OnInspectorGUI()
        {
            var palette = targets.Length == 1 ? (ColorPalette)target : null;
            if (palette != null)
                EnsureUsageScan(palette);

            serializedObject.Update();

            DrawColors(palette);
            EditorGUILayout.Space();
            DrawStyles();

            serializedObject.ApplyModifiedProperties();

            if (palette == null)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            var rect = GUILayoutUtility.GetRect(100f, 200f, GUILayout.ExpandWidth(true));
            DrawPreview(rect, palette);
        }

        // --- colors list ------------------------------------------------------

        void DrawColors(ColorPalette palette)
        {
            var colorsProp = serializedObject.FindProperty("_colors");
            if (colorsProp == null)
                return;

            EditorGUILayout.LabelField($"Palette colors ({colorsProp.arraySize})", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Each color has a stable id (shown first) that references point at, so colors can be " +
                "added, removed and reordered freely. Hiding keeps a color working but out of pickers.",
                MessageType.None);

            int removeAt = -1;
            int moveFrom = -1;
            int moveTo = -1;

            for (int i = 0; i < colorsProp.arraySize; i++)
            {
                var entry = colorsProp.GetArrayElementAtIndex(i);
                var idProp = entry.FindPropertyRelative("_id");
                var nameProp = entry.FindPropertyRelative("name");
                var colorProp = entry.FindPropertyRelative("color");
                var hasContrastProp = entry.FindPropertyRelative("hasContrast");
                var contrastProp = entry.FindPropertyRelative("contrast");
                var hiddenProp = entry.FindPropertyRelative("hidden");

                int id = idProp.intValue;
                int usages = _usage != null ? _usage.Count(id) : 0;

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(id.ToString(), EditorStyles.miniLabel, GUILayout.Width(22f));

                    nameProp.stringValue = EditorGUILayout.TextField(nameProp.stringValue, GUILayout.MinWidth(70f));

                    EditorGUI.BeginChangeCheck();
                    var color = EditorGUILayout.ColorField(GUIContent.none, colorProp.colorValue,
                        false, false, true, GUILayout.Width(48f));
                    if (EditorGUI.EndChangeCheck())
                        colorProp.colorValue = color;

                    var onToggle = new GUIContent("on", "Contrast color: the readable foreground drawn on top of this color (e.g. text on a button). " +
                        "When off, one is derived from this color's luminance.");
                    hasContrastProp.boolValue = EditorGUILayout.ToggleLeft(onToggle, hasContrastProp.boolValue, GUILayout.Width(34f));

                    using (new EditorGUI.DisabledScope(!hasContrastProp.boolValue))
                    {
                        EditorGUI.BeginChangeCheck();
                        var contrast = EditorGUILayout.ColorField(
                            new GUIContent(string.Empty, "Contrast (on-) color for this palette entry."),
                            contrastProp.colorValue, false, false, true, GUILayout.Width(48f));
                        if (EditorGUI.EndChangeCheck())
                        {
                            contrastProp.colorValue = contrast;
                            hasContrastProp.boolValue = true;
                        }
                    }

                    hiddenProp.boolValue = EditorGUILayout.ToggleLeft("hidden", hiddenProp.boolValue, GUILayout.Width(58f));

                    var usageText = usages == 0 ? "unused" : "×" + usages;
                    string usageTooltip = _usage == null
                        ? "Not scanned yet."
                        : usages == 0
                            ? "Not referenced anywhere - safe to delete."
                            : _usage.Describe(id);
                    var usageContent = new GUIContent(usageText, usageTooltip);
                    var previousColor = GUI.contentColor;
                    GUI.contentColor = usages == 0 ? new Color(0.6f, 0.6f, 0.6f, 1f) : (Color)new Color32(126, 200, 120, 255);
                    GUILayout.Label(usageContent, EditorStyles.miniLabel, GUILayout.Width(46f));
                    GUI.contentColor = previousColor;

                    using (new EditorGUI.DisabledScope(i == 0))
                    {
                        if (GUILayout.Button("↑", GUILayout.Width(20f)))
                        {
                            moveFrom = i;
                            moveTo = i - 1;
                        }
                    }

                    using (new EditorGUI.DisabledScope(i == colorsProp.arraySize - 1))
                    {
                        if (GUILayout.Button("↓", GUILayout.Width(20f)))
                        {
                            moveFrom = i;
                            moveTo = i + 1;
                        }
                    }

                    if (GUILayout.Button("✕", GUILayout.Width(22f)))
                        removeAt = i;
                }
            }

            if (moveFrom >= 0)
                colorsProp.MoveArrayElement(moveFrom, moveTo);

            if (removeAt >= 0)
            {
                var entry = colorsProp.GetArrayElementAtIndex(removeAt);
                string entryName = entry.FindPropertyRelative("name").stringValue;
                int entryId = entry.FindPropertyRelative("_id").intValue;
                int entryUsages = _usage != null ? _usage.Count(entryId) : 0;

                string detail = entryUsages == 0
                    ? "Nothing references it, so this is safe."
                    : $"{entryUsages} reference(s) point at it — they will stop resolving. " +
                      "Tick 'hidden' instead to keep the color working but out of pickers.\n\n" +
                      _usage.Describe(entryId, 4);

                if (EditorUtility.DisplayDialog($"Remove '{entryName}' (id {entryId})", detail, "Remove", "Cancel"))
                    colorsProp.DeleteArrayElementAtIndex(removeAt);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add color"))
                {
                    int newId = 0;
                    for (int i = 0; i < colorsProp.arraySize; i++)
                        newId = Mathf.Max(newId, colorsProp.GetArrayElementAtIndex(i).FindPropertyRelative("_id").intValue + 1);

                    int index = colorsProp.arraySize;
                    colorsProp.InsertArrayElementAtIndex(index);
                    var entry = colorsProp.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("_id").intValue = newId;
                    entry.FindPropertyRelative("name").stringValue = "Color " + newId;
                    entry.FindPropertyRelative("color").colorValue = Color.grey;
                    entry.FindPropertyRelative("hasContrast").boolValue = false;
                    entry.FindPropertyRelative("contrast").colorValue = Color.white;
                    entry.FindPropertyRelative("hidden").boolValue = false;
                }

                if (GUILayout.Button("Rescan usages"))
                    _usage = null;
            }

            if (_usage != null && _usage.missingIds.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.Append("References point at colors this palette does not define (they render pure magenta): ");
                for (int i = 0; i < _usage.missingIds.Count; i++)
                {
                    if (i > 0)
                        sb.Append(", ");
                    int id = _usage.missingIds[i];
                    sb.Append("id ").Append(id).Append(" ×").Append(_usage.Count(id));
                }

                EditorGUILayout.HelpBox(sb.ToString(), MessageType.Warning);
            }

            DrawUsageBreakdown(palette);
        }

        /// <summary>
        /// Expandable per-role usage list: one foldout per referenced color, then one row per
        /// asset with its count, so a 255-reference color is readable at a glance.
        /// </summary>
        void DrawUsageBreakdown(ColorPalette palette)
        {
            if (palette == null || _usage == null)
                return;

            var referenced = new List<PaletteColor>();
            for (int i = 0; i < palette.count; i++)
            {
                if (_usage.Count(palette.colors[i].id) > 0)
                    referenced.Add(palette.colors[i]);
            }

            if (referenced.Count == 0)
                return;

            referenced.Sort((a, b) => _usage.Count(b.id).CompareTo(_usage.Count(a.id)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Usages", EditorStyles.boldLabel);

            foreach (var color in referenced)
            {
                int total = _usage.Count(color.id);
                var usages = _usage.Usages(color.id);

                string header = $"{color.id}  {color.name}   —   {total} in {usages.Count} asset(s)";
                bool expanded = _expandedColor == color.id;

                using (new EditorGUILayout.HorizontalScope())
                {
                    var swatch = GUILayoutUtility.GetRect(14f, 14f, GUILayout.Width(14f));
                    EditorGUI.DrawRect(new Rect(swatch.x, swatch.y + 2f, 12f, 12f), color.color);

                    if (GUILayout.Button(expanded ? "▾  " + header : "▸  " + header, EditorStyles.label))
                        _expandedColor = expanded ? -1 : color.id;
                }

                if (!expanded)
                    continue;

                EditorGUI.indentLevel++;
                foreach (var usage in usages)
                {
                    string locations = usage.locations.Count > 0 ? string.Join("\n", usage.locations) : null;
                    var content = new GUIContent($"{usage.assetName}   ×{usage.count}", locations);
                    EditorGUILayout.LabelField(content, EditorStyles.miniLabel);
                }
                EditorGUI.indentLevel--;
            }
        }

        void DrawStyles()
        {
            var stylesProp = serializedObject.FindProperty("_styles");
            if (stylesProp != null)
                EditorGUILayout.PropertyField(stylesProp, new GUIContent("Default Styles"), true);
        }

        // --- preview ----------------------------------------------------------

        static void DrawPreview(Rect rect, ColorPalette palette)
        {
            // Scene: background, then a scrim covering part of it (Overlay).
            EditorGUI.DrawRect(rect, RoleOr(palette, (int)ColorType.Background, new Color(0.06f, 0.06f, 0.06f, 1f)));
            var scrim = new Rect(rect.x, rect.y, rect.width * 0.38f, rect.height);
            EditorGUI.DrawRect(scrim, RoleOr(palette, ProjectRoleOverlay, new Color(0f, 0f, 0f, 0.72f)));

            const float outerPad = 12f;
            var card = new Rect(rect.x + outerPad, rect.y + outerPad,
                                rect.width - outerPad * 2f, rect.height - outerPad * 2f);

            // Shadow -> border ring -> card face.
            DrawRoundedRect(new Rect(card.x + 3f, card.y + 4f, card.width, card.height),
                RoleOr(palette, ProjectRoleShadow, new Color(0f, 0f, 0f, 0.5f)), palette.styles.panelRoundness);
            DrawRoundedRect(new Rect(card.x - 1f, card.y - 1f, card.width + 2f, card.height + 2f),
                RoleOr(palette, ProjectRoleBorder, new Color(0.44f, 0.49f, 0.47f, 0.35f)), palette.styles.panelRoundness);
            DrawRoundedRect(card, RoleOr(palette, (int)ColorType.Surface, new Color(0.14f, 0.14f, 0.14f, 1f)), palette.styles.panelRoundness);

            const float innerPad = 10f;
            float x = card.x + innerPad;
            float y = card.y + innerPad;
            float contentWidth = card.width - innerPad * 2f;

            var surfaceFg = RoleContrastOr(palette, (int)ColorType.Surface, new Color(0.8f, 0.8f, 0.8f, 1f));
            var muted = RoleOr(palette, (int)ColorType.Muted, surfaceFg * 0.7f);

            DrawBar(x, y, contentWidth * 0.60f, 8f, surfaceFg);
            y += 14f;

            DrawBar(x, y, contentWidth * 0.45f, 5f, muted);
            y += 10f;

            DrawBar(x, y, contentWidth * 0.30f, 5f, surfaceFg);
            y += 16f;

            const float buttonHeight = 26f;
            float buttonWidth = contentWidth * 0.38f;
            var button = new Rect(x, y, buttonWidth, buttonHeight);
            DrawRoundedRect(button, RoleOr(palette, (int)ColorType.Accent, new Color(0.25f, 0.48f, 0.45f, 1f)), palette.styles.buttonRoundness);
            DrawBar(button.x + 10f, button.y + button.height * 0.5f - 3f,
                    button.width - 20f, 6f, RoleContrastOr(palette, (int)ColorType.Accent, new Color(0.9f, 0.93f, 0.92f, 1f)));
            y += buttonHeight + 12f;

            // Bar: track, damage trail, fill (the health bar case).
            var bar = new Rect(x, y, contentWidth, 12f);
            DrawRoundedRect(bar, RoleOr(palette, (int)ColorType.Black, new Color(0.02f, 0.03f, 0.04f, 1f)), palette.styles.panelRoundness);
            DrawRoundedRect(new Rect(bar.x, bar.y, bar.width * 0.70f, bar.height),
                RoleOr(palette, ProjectRoleDamage, new Color(0.3f, 0.13f, 0.13f, 1f)), palette.styles.panelRoundness);
            DrawRoundedRect(new Rect(bar.x, bar.y, bar.width * 0.55f, bar.height),
                RoleOr(palette, (int)ColorType.Success, new Color(0.43f, 0.61f, 0.45f, 1f)), palette.styles.panelRoundness);
            y += bar.height + 12f;

            const float chipGap = 6f;
            float chipWidth = (contentWidth - chipGap * 2f) / 3f;
            const float chipHeight = 22f;
            DrawChip(new Rect(x, y, chipWidth, chipHeight), RoleOr(palette, (int)ColorType.Success, new Color(0.43f, 0.61f, 0.45f, 1f)),
                RoleContrastOr(palette, (int)ColorType.Success, new Color(0.1f, 0.1f, 0.1f, 1f)), palette.styles.buttonRoundness);
            DrawChip(new Rect(x + chipWidth + chipGap, y, chipWidth, chipHeight), RoleOr(palette, (int)ColorType.Warning, new Color(0.73f, 0.56f, 0.29f, 1f)),
                RoleContrastOr(palette, (int)ColorType.Warning, new Color(0.1f, 0.1f, 0.1f, 1f)), palette.styles.buttonRoundness);
            DrawChip(new Rect(x + (chipWidth + chipGap) * 2f, y, chipWidth, chipHeight), RoleOr(palette, (int)ColorType.Danger, new Color(0.63f, 0.24f, 0.22f, 1f)),
                RoleContrastOr(palette, (int)ColorType.Danger, new Color(0.93f, 0.9f, 0.89f, 1f)), palette.styles.buttonRoundness);
        }

        static Color RoleOr(ColorPalette palette, int id, Color fallback)
        {
            return palette.IsValid(id) ? palette.GetColor(id) : fallback;
        }

        static Color RoleContrastOr(ColorPalette palette, int id, Color fallback)
        {
            if (palette.IsValid(id))
                return palette.GetContrast(id);

            float luminance = 0.2126f * fallback.r + 0.7152f * fallback.g + 0.0722f * fallback.b;
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
