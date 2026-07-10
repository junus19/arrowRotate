using UnityEditor;
using UnityEngine;

namespace GameBrain.Editor
{
    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    public class HideIfPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HideIfAttribute hideIfAttribute = (HideIfAttribute)attribute;
            SerializedProperty conditionalProperty = property.serializedObject.FindProperty(hideIfAttribute.ConditionalPropertyName);
            if (conditionalProperty != null)
            {
                bool conditionMet = ComparePropertyToValue(conditionalProperty, hideIfAttribute.CompareValue);
                if (!conditionMet) EditorGUI.PropertyField(position, property, label, true);
            }
            else EditorGUI.LabelField(position, label.text, "Conditional property not found");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            HideIfAttribute hideIfAttribute = (HideIfAttribute)attribute;
            SerializedProperty conditionalProperty = property.serializedObject.FindProperty(hideIfAttribute.ConditionalPropertyName);
            if (conditionalProperty == null) return EditorGUIUtility.singleLineHeight;
            bool conditionMet = ComparePropertyToValue(conditionalProperty, hideIfAttribute.CompareValue);
            return !conditionMet ? EditorGUI.GetPropertyHeight(property, label) : 0;
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
