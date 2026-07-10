using System;
using UnityEditor;
using UnityEngine;

namespace GameBrain.Editor
{
    [CustomPropertyDrawer(typeof(DynamicRangeAttribute))]
    public class DynamicRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DynamicRangeAttribute rangeAttribute = (DynamicRangeAttribute)attribute;

            if (string.IsNullOrEmpty(rangeAttribute.ScriptableObjectTypeName) || string.IsNullOrEmpty(rangeAttribute.ListPropertyName)) return;
            string[] guids = AssetDatabase.FindAssets($"t:{rangeAttribute.ScriptableObjectTypeName}");

            int max = 0;

            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                ScriptableObject scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);

                if (scriptableObject != null)
                {
                    SerializedObject serializedObject = new SerializedObject(scriptableObject);
                    SerializedProperty listProp = serializedObject.FindProperty(rangeAttribute.ListPropertyName);

                    if (listProp != null && listProp.isArray)
                    {
                        max = Mathf.Max(0, listProp.arraySize - 1);
                    }
                }
            }

            property.intValue = Math.Clamp(property.intValue, 0, max);
            property.intValue = EditorGUI.IntSlider(position, label, property.intValue, 0, max);
        }
    }
}
