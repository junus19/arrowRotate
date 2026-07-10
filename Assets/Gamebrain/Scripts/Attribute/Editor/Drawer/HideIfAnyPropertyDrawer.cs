using UnityEditor;
using UnityEngine;

namespace GameBrain.Editor
{
    [CustomPropertyDrawer(typeof(HideIfAnyAttribute))]
    public class HideIfAnyPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            HideIfAnyAttribute hideIfAnyAttribute = (HideIfAnyAttribute)attribute;
            SerializedProperty conditionalProperty = property.serializedObject.FindProperty(hideIfAnyAttribute.ConditionalPropertyName);
            if (conditionalProperty != null)
            {
                bool conditionMet = true;
                foreach (object comparedValue in hideIfAnyAttribute.CompareValue)
                {
                    conditionMet = ComparePropertyToValue(conditionalProperty, comparedValue);
                    if (conditionMet) break;
                }
                if (!conditionMet) EditorGUI.PropertyField(position, property, label, true);
            }
            else EditorGUI.LabelField(position, label.text, "Conditional property not found");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            HideIfAnyAttribute hideIfAnyAttribute = (HideIfAnyAttribute)attribute;
            SerializedProperty conditionalProperty = property.serializedObject.FindProperty(hideIfAnyAttribute.ConditionalPropertyName);
            if (conditionalProperty == null) return EditorGUIUtility.singleLineHeight;
            bool conditionMet = true;
            foreach (object comparedValue in hideIfAnyAttribute.CompareValue)
            {
                conditionMet = ComparePropertyToValue(conditionalProperty, comparedValue);
                if (conditionMet) break;
            }

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
