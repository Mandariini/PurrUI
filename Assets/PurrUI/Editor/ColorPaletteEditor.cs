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
            DrawDefaultInspector();

            if (targets.Length != 1)
                return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

            var palette = (ColorPalette)target;
            var rect = GUILayoutUtility.GetRect(100f, 180f, GUILayout.ExpandWidth(true));
            DrawPreview(rect, palette);
        }

        static void DrawPreview(Rect rect, ColorPalette palette)
        {
            EditorGUI.DrawRect(rect, palette.GetColor(ColorType.Background));

            const float outerPad = 12f;
            var card = new Rect(rect.x + outerPad, rect.y + outerPad,
                                rect.width - outerPad * 2f, rect.height - outerPad * 2f);
            EditorGUI.DrawRect(card, palette.GetColor(ColorType.Surface));

            const float innerPad = 10f;
            float x = card.x + innerPad;
            float y = card.y + innerPad;
            float contentWidth = card.width - innerPad * 2f;

            var surfaceFg = palette.GetContrast(ColorType.Surface);
            var muted = palette.GetColor(ColorType.Muted);

            DrawBar(x, y, contentWidth * 0.60f, 8f, surfaceFg);
            y += 14f;

            DrawBar(x, y, contentWidth * 0.45f, 5f, muted);
            y += 10f;

            DrawBar(x, y, contentWidth * 0.30f, 5f, surfaceFg);
            y += 18f;

            const float buttonHeight = 26f;
            float buttonWidth = contentWidth * 0.38f;
            var button = new Rect(x, y, buttonWidth, buttonHeight);
            EditorGUI.DrawRect(button, palette.GetColor(ColorType.Accent));
            DrawBar(button.x + 10f, button.y + button.height * 0.5f - 3f,
                    button.width - 20f, 6f, palette.GetContrast(ColorType.Accent));
            y += buttonHeight + 12f;

            const float chipGap = 6f;
            float chipWidth = (contentWidth - chipGap * 2f) / 3f;
            const float chipHeight = 22f;
            DrawChip(new Rect(x, y, chipWidth, chipHeight),
                palette.GetColor(ColorType.Success), palette.GetContrast(ColorType.Success));
            DrawChip(new Rect(x + chipWidth + chipGap, y, chipWidth, chipHeight),
                palette.GetColor(ColorType.Warning), palette.GetContrast(ColorType.Warning));
            DrawChip(new Rect(x + (chipWidth + chipGap) * 2f, y, chipWidth, chipHeight),
                palette.GetColor(ColorType.Danger), palette.GetContrast(ColorType.Danger));
        }

        static void DrawBar(float x, float y, float w, float h, Color c)
        {
            EditorGUI.DrawRect(new Rect(x, y, w, h), c);
        }

        static void DrawChip(Rect rect, Color background, Color foreground)
        {
            EditorGUI.DrawRect(rect, background);
            DrawBar(rect.x + 6f, rect.y + rect.height * 0.5f - 2f,
                    rect.width - 12f, 4f, foreground);
        }
    }
}
