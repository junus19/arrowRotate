using UnityEditor;
using UnityEditor.Toolbars;
using GameBrain.Utils.Editor;

namespace Gamebrain.Scripts.Editor
{
    public static class MainToolbarAddon_BootToggle
    {
        [MainToolbarElement("GameBrain/Bootstrap Toggle", defaultDockPosition = MainToolbarDockPosition.Middle, menuPriority = -1)]
        public static MainToolbarElement BootstrapToggle()
        {
            MainToolbarContent content = new MainToolbarContent(EditorGUIUtility.TrTextContent("Bootstrap").text, tooltip: "Starts the game from the initial scene.");
            return new MainToolbarToggle(content, EditorConfig.GetOrCreateSettings().RedirectToFirstScene, (newValue) => { EditorConfig.GetOrCreateSettings().RedirectToFirstScene = newValue; });
        }
    }
}
