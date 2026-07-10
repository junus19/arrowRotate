using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameBrain.Editor
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(DropdownAttribute))]
    public class DropdownDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DropdownAttribute dropdown = (DropdownAttribute)attribute;

            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                {
                    int index = System.Array.IndexOf(dropdown.Options, property.stringValue);
                    if (index < 0) index = 0;

                    index = EditorGUI.Popup(position, label.text, index, dropdown.Options);
                    property.stringValue = dropdown.Options[index];
                    break;
                }
                case SerializedPropertyType.Integer:
                {
                    int index = property.intValue;
                    if (index < 0 || index >= dropdown.Options.Length) index = 0;

                    index = EditorGUI.Popup(position, label.text, index, dropdown.Options);
                    property.intValue = index;
                    break;
                }
                default:
                    EditorGUI.LabelField(position, label.text, "Use Dropdown with string or int.");
                    break;
            }
        }
    }
#endif
}
