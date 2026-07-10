using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GameBrain.Utils.Editor
{
    public static class EditorSceneUtility
    {
        public static SceneAsset GetFirstScene()
        {
            if (EditorBuildSettings.scenes.Length == 0) return null;
            // string pathOfFirstScene = EditorBuildSettings.scenes.First(scene => scene.path.Contains("Boot")).path;
            string pathOfFirstScene = EditorBuildSettings.scenes[0].path;
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(pathOfFirstScene);
        }

        public static void SetStartScene(SceneAsset sceneAsset) => EditorSceneManager.playModeStartScene = sceneAsset;
    }
}
