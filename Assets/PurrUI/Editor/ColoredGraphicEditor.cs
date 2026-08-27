using System;
using PurrNet.UI;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    [CustomEditor(typeof(ColoredGraphic))]
    [CanEditMultipleObjects]
    public class ColoredGraphicEditor : UnityEditor.Editor
    {
        SerializedProperty _graphicProp;
        SerializedProperty _transitionDurationProp;
        SerializedProperty _colorProp;
        SerializedProperty _coloredInfosProp;

        void OnEnable()
        {
            _graphicProp = serializedObject.FindProperty("_graphic");
            _transitionDurationProp = serializedObject.FindProperty("_transitionDuration");
            _colorProp = serializedObject.FindProperty("_color");
            _coloredInfosProp = serializedObject.FindProperty("_coloredInfos");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_graphicProp);
            EditorGUILayout.PropertyField(_transitionDurationProp);

            if (_graphicProp.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Multi-selection has different Graphic targets — edit individually.", MessageType.Info);
                ApplyAndRefresh();
                return;
            }

            var graphic = _graphicProp.objectReferenceValue;

            if (graphic == null)
            {
                EditorGUILayout.HelpBox("Assign a Graphic to color.", MessageType.Info);
                ApplyAndRefresh();
                return;
            }

            if (graphic is IColored colored)
                DrawMultiKey(colored);
            else
                EditorGUILayout.PropertyField(_colorProp, new GUIContent("Color"));

            DrawPaletteSource();

            ApplyAndRefresh();
        }

        void DrawPaletteSource()
        {
            if (targets.Length != 1)
                return;

            var colored = (ColoredGraphic)target;
            var active = colored.activePalette;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Palette Source", EditorStyles.boldLabel);

            if (active == null)
            {
                EditorGUILayout.HelpBox(
                    "No palette found in the hierarchy and no global palette settings.\n" +
                    "Add a PaletteProvider above this object or create global settings via PurrUI > Global Palette Settings.",
                    MessageType.Warning);
                return;
            }

            EditorGUILayout.ObjectField("Active Palette", active, typeof(ColorPalette), false);

            if (colored.isUsingGlobalPalette)
                EditorGUILayout.HelpBox(
                    "Resolved from the global palette settings - no IPaletteProvider in the hierarchy.",
                    MessageType.Info);
        }

        void DrawMultiKey(IColored colored)
        {
            var keys = colored.keys ?? Array.Empty<string>();

            if (_coloredInfosProp.arraySize != keys.Length)
                _coloredInfosProp.arraySize = keys.Length;

            if (keys.Length == 0)
            {
                EditorGUILayout.HelpBox("Target exposes no color keys.", MessageType.Info);
                return;
            }

            for (int i = 0; i < keys.Length; i++)
            {
                var element = _coloredInfosProp.GetArrayElementAtIndex(i);
                var label = new GUIContent($"[{i}] {keys[i]}", $"Index {i} — use SetColor({i}, ...) at runtime.");
                EditorGUILayout.PropertyField(element, label, true);
            }
        }

        void ApplyAndRefresh()
        {
            if (!serializedObject.ApplyModifiedProperties())
                return;

            foreach (var obj in targets)
            {
                if (obj is not ColoredGraphic coloredGraphic) continue;
                coloredGraphic.Refresh();
                EditorUtility.SetDirty(coloredGraphic);
            }
        }
    }
}
