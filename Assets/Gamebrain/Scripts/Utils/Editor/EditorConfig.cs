using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameBrain.Utils.Editor
{
    public class EditorConfig : ScriptableObject
    {
        [Header("Scene")]
        [Tooltip("On start, redirect the game to the first scene.")]
        [SerializeField] private bool _redirectToFirstScene = true;

        protected const string _path = "Assets/Gamebrain/Settings/EditorConfig.asset";

        public bool RedirectToFirstScene
        {
            get => _redirectToFirstScene;
            set
            {
                _redirectToFirstScene = value;
                OnValidate();
            }
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            EditorBuildSettings.sceneListChanged -= OnSceneListChanged;
            EditorBuildSettings.sceneListChanged += OnSceneListChanged;
        }

        public static EditorConfig GetOrCreateSettings()
        {
            EditorConfig settings = AssetDatabase.LoadAssetAtPath<EditorConfig>(_path);

            if (settings != null)
                return settings;

            if (!Directory.Exists("Assets/Gamebrain/Settings/"))
                Directory.CreateDirectory("Assets/Gamebrain/Settings/");

            settings = CreateInstance<EditorConfig>();
            AssetDatabase.CreateAsset(settings, _path);
            AssetDatabase.SaveAssets();
            return settings;
        }

        private static void OnSceneListChanged()
        {
            GetOrCreateSettings().OnValidate();
        }

        private void OnValidate()
        {
            EditorSceneUtility.SetStartScene(_redirectToFirstScene ? EditorSceneUtility.GetFirstScene() : null);
        }
    }
}
