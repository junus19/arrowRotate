using UnityEngine;
using UnityEditor;

namespace GameBrain.Editor
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;
            SerializedProperty conditionalProperty = property.serializedObject.FindProperty(showIf.ConditionalPropertyName);
            if (conditionalProperty != null)
            {
                bool conditionMet = ComparePropertyToValue(conditionalProperty, showIf.CompareValue);
                if (conditionMet)
                {
                    // if (!string.IsNullOrEmpty(showIf.Header))
                    // {
                    //     EditorGUI.LabelField(position, showIf.Header,  EditorStyles.foldoutHeader);
                    //     position.y += EditorGUIUtility.singleLineHeight;
                    // }
                    EditorGUI.PropertyField(position, property, label, true);
                }
            }
            else EditorGUI.LabelField(position, label.text, "Conditional property not found");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;
            SerializedProperty conditionalProperty = property.serializedObject.FindProperty(showIf.ConditionalPropertyName);
            if (conditionalProperty == null) return EditorGUIUtility.singleLineHeight;
            bool conditionMet = ComparePropertyToValue(conditionalProperty, showIf.CompareValue);
            return conditionMet ? EditorGUI.GetPropertyHeight(property, label) : 0;
        }

        private bool ComparePropertyToValue(SerializedProperty property, object value)
            => property.propertyType switch
            {
                SerializedPropertyType.Boolean => property.boolValue.Equals(value),
                SerializedPropertyType.Enum => property.enumValueIndex.Equals((int)value),
                SerializedPropertyType.Float => property.floatValue.Equals((float)value),
                SerializedPropertyType.Integer => property.intValue.Equals((int)value),
                SerializedPropertyType.String => property.stringValue.Equals((string)value),
                _ => false
            };
    }
}
