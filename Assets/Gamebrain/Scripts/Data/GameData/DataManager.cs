using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameBrain.Casual.Editor
{
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }


        private string GetDataPath()
        {
            var dataPath = Path.Combine(Application.persistentDataPath);
            return dataPath;
        }

        public void ClearAllData()
        {
            PlayerPrefs.DeleteAll();

            string[] filePaths = Directory.GetFiles(Application.persistentDataPath);
            foreach (string filePath in filePaths)
                File.Delete(filePath);
        }

        public void RevealInFinder()
        {
            var dataPath = GetDataPath();
#if UNITY_EDITOR
            EditorUtility.RevealInFinder(dataPath);
#endif
        }
    }
#if UNITY_EDITOR
    [CustomEditor(typeof(DataManager))]
    public class DataManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            DataManager myScript = (DataManager)target;

            if (GUILayout.Button("Clean"))
            {
                myScript.ClearAllData();
            }

            if (GUILayout.Button("RevealInFinder"))
            {
                myScript.RevealInFinder();
            }
        }
    }
#endif
}
