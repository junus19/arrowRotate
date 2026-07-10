using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GameBrain
{
    [CustomPropertyDrawer(typeof(SceneDropdownAttribute))]
    public class SceneDropdownDrawer : PropertyDrawer
    {
        private readonly List<string> _sceneNames = new List<string>();
        private readonly List<string> _scenePaths = new List<string>();
        private bool _initialized;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (!_initialized)
            {
                RefreshSceneList();
                _initialized = true;
            }

            EditorGUI.BeginProperty(position, label, property);

            switch (property.propertyType)
            {
                case SerializedPropertyType.String:
                    DrawSceneDropdown(position, property, label);
                    break;
                case SerializedPropertyType.Integer:
                    DrawSceneIndexDropdown(position, property, label);
                    break;
                default:
                    EditorGUI.LabelField(position, label.text, "Use [SceneDropdown] with string or int fields only");
                    break;
            }

            EditorGUI.EndProperty();
        }

        private void DrawSceneDropdown(Rect position, SerializedProperty property, GUIContent label)
        {
            string currentValue = property.stringValue;
            int selectedIndex = GetSceneIndex(currentValue);

            string[] options = new string[_sceneNames.Count + 1];
            options[0] = "None";
            for (int i = 0; i < _sceneNames.Count; i++)
            {
                options[i + 1] = _sceneNames[i];
            }

            selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex + 1, options) - 1;

            property.stringValue = selectedIndex >= 0 && selectedIndex < _sceneNames.Count ? _sceneNames[selectedIndex] : "";
        }

        private void DrawSceneIndexDropdown(Rect position, SerializedProperty property, GUIContent label)
        {
            int currentValue = property.intValue;
            int selectedIndex = currentValue;

            if (selectedIndex < -1 || selectedIndex >= _sceneNames.Count)
                selectedIndex = -1;

            string[] options = new string[_sceneNames.Count + 1];
            options[0] = "None (-1)";
            for (int i = 0; i < _sceneNames.Count; i++)
            {
                options[i + 1] = $"{i}: {_sceneNames[i]}";
            }

            selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex + 1, options) - 1;

            property.intValue = selectedIndex;
        }

        private int GetSceneIndex(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return -1;

            for (int i = 0; i < _sceneNames.Count; i++)
            {
                if (_sceneNames[i] == sceneName)
                    return i;
            }

            return -1;
        }

        private void RefreshSceneList()
        {
            _sceneNames.Clear();
            _scenePaths.Clear();

            SceneDropdownAttribute sceneDropdownAttribute = (SceneDropdownAttribute)attribute;
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (!sceneDropdownAttribute.IncludeDisabledScenes && !scene.enabled) continue;
                string scenePath = scene.path;
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

                _sceneNames.Add(sceneName);
                _scenePaths.Add(scenePath);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}
