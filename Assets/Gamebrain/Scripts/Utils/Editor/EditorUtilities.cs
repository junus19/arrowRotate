using UnityEditor;

namespace GameBrain.Utils.Editor
{
    [InitializeOnLoad]
    public static class EditorUtilities
    {
        private static readonly EditorConfig _config;
        
        static EditorUtilities()
        {
            _config = EditorConfig.GetOrCreateSettings();
        }
    }
}
