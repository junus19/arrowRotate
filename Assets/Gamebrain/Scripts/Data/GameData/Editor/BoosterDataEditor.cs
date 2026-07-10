using UnityEngine;
using UnityEditor;

namespace GameBrain.Casual.Editor
{
    [CustomEditor(typeof(BoosterGameData))]
    public class BoosterDataEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            BoosterGameData baseData = (BoosterGameData)target;

            if (GUILayout.Button("Add 10 Hammer"))
            {
                baseData.AddBooster(BoosterType.Hammer, 10);
            }
            
            if (GUILayout.Button("Add 10 Refresh"))
            {
                baseData.AddBooster(BoosterType.Refresh, 10);
            }
            
            if (GUILayout.Button("Add 10 Swap"))
            {
                baseData.AddBooster(BoosterType.Swap, 10);
            }
            
            if (GUILayout.Button("Add 10 Picker"))
            {
                baseData.AddBooster(BoosterType.GroupRemove, 10);
            }

            if (GUILayout.Button("Clear"))
            {
                //PlayerPrefs.DeleteAll();
                baseData.ClearData();
            }
        }
    }
}
