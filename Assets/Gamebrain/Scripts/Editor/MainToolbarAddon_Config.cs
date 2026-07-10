using UnityEditor;
using GameBrain.Casual;
using UnityEditor.Toolbars;

namespace Gamebrain.Scripts.Editor
{
    public static class MainToolbarAddon_Config
    {
        [MainToolbarElement("GameBrain/Config Button", defaultDockPosition = MainToolbarDockPosition.Middle, menuPriority = -1)]
        public static MainToolbarElement ConfigButton()
        {
            MainToolbarContent content = new MainToolbarContent(EditorGUIUtility.TrTextContent("Open Config").text, tooltip: "Opens config data.");
            return new MainToolbarButton(content, () =>
                {
                    string[] guids = AssetDatabase.FindAssets($"t:{typeof(GameConfig).Name}");

                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        GameConfig asset = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
                        if (asset == null)
                            continue;
                        Selection.activeObject = asset;
                        return;
                    }
                }
            );
        }
    }
}
