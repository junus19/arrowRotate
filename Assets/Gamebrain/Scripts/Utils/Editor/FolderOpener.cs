using System;
using UnityEditor;
using UnityEngine;

namespace GAME.Scripts.GameData
{
    public static class FolderOpener
    {
        public static void OpenFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath))
            {
                Debug.LogError("Folder path is empty!");
                return;
            }

            if (!System.IO.Directory.Exists(folderPath))
            {
                Debug.LogError($"Folder does not exist: {folderPath}");
                return;
            }

            try
            {
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
                System.Diagnostics.Process.Start("explorer.exe", folderPath);
#elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
                EditorUtility.RevealInFinder(folderPath);
#elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
                System.Diagnostics.Process.Start("xdg-open", folderPath);
#else
				UnityEngine.Debug.LogWarning("Platform not supported for opening folders");
#endif
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to open folder: {e.Message}");
            }
        }

        [MenuItem("Tools/Open Save Path")]
        public static void OpenPersistentDataPath()
        {
            OpenFolder(Application.persistentDataPath);
        }

        // Opens the streaming assets path
        public static void OpenStreamingAssetsPath()
        {
            OpenFolder(Application.streamingAssetsPath);
        }

        // Opens the data path
        public static void OpenDataPath()
        {
            OpenFolder(Application.dataPath);
        }
    }
}