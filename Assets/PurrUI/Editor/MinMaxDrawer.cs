using PurrNet.UI;
using UnityEditor;
using UnityEngine;

namespace PurrNet.Editor.UI
{
    [CustomPropertyDrawer(typeof(MinMaxAttribute))]
    public class MinMaxDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (attribute is not MinMaxAttribute minMax)
                return;

            using (new EditorGUI.PropertyScope(position, label, property))
            {
                if (property.propertyType != SerializedPropertyType.Vector2)
                {
                    EditorGUI.LabelField(position, label.text, "Use [MinMax] with Vector2");
                    return;
                }

                float minValue = property.vector2Value.x;
                float maxValue = property.vector2Value.y;
                float minLimit = minMax.minLimit;
                float maxLimit = minMax.maxLimit;

                var controlRect = EditorGUI.PrefixLabel(position, label);

                const float fieldWidth = 50f;
                const float spacing = 4f;

                var minFieldRect = new Rect(controlRect.x, controlRect.y, fieldWidth, controlRect.height);
                var maxFieldRect = new Rect(controlRect.xMax - fieldWidth, controlRect.y, fieldWidth, controlRect.height);
                var sliderRect = new Rect(
                    minFieldRect.xMax + spacing,
                    controlRect.y,
                    controlRect.width - (fieldWidth * 2f) - (spacing * 2f),
                    controlRect.height);

                int previousIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                EditorGUI.BeginChangeCheck();
                float newMin = EditorGUI.FloatField(minFieldRect, minValue);
                if (EditorGUI.EndChangeCheck())
                {
                    minValue = Mathf.Clamp(newMin, minLimit, maxLimit);
                    if (minValue > maxValue) maxValue = minValue;
                }

                EditorGUI.BeginChangeCheck();
                float newMax = EditorGUI.FloatField(maxFieldRect, maxValue);
                if (EditorGUI.EndChangeCheck())
                {
                    maxValue = Mathf.Clamp(newMax, minLimit, maxLimit);
                    if (maxValue < minValue) minValue = maxValue;
                }

                EditorGUI.MinMaxSlider(sliderRect, ref minValue, ref maxValue, minLimit, maxLimit);

                EditorGUI.indentLevel = previousIndent;

                property.vector2Value = new Vector2(minValue, maxValue);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (attribute is not MinMaxAttribute minMax)
                return base.GetPropertyHeight(property, label);

            return EditorGUIUtility.singleLineHeight;
        }
    }
}
