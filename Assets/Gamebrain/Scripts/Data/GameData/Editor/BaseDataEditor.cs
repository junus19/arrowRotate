using UnityEngine;
using UnityEditor;
using GAME.Scripts.GameData;
using GameBrain.Casual;

//[CustomEditor(typeof(BaseGameData<>), true)]
//public class BaseDataEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        base.OnInspectorGUI();
//        GameData baseData = (GameData)target;
//        if (GUILayout.Button("Clear"))
//        {
//            PlayerPrefs.DeleteAll();
//            baseData.ClearData();
//        }
//    }
//}



//namespace WaterSort.Scripts.Editor
//{
//    [CustomEditor(typeof(GameData))]
//    public class BaseDataEditor : UnityEditor.Editor
//    {
//        public override void OnInspectorGUI()
//        {
//            base.OnInspectorGUI();
//            GameData baseData = (GameData)target;
//            if (GUILayout.Button("Clear"))
//            {
//                PlayerPrefs.DeleteAll();
//                baseData.ClearData();
//            }
//        }
//    }
//}
namespace WaterSort.Scripts.Editor
{
   [CustomEditor(typeof(GameData))]
   public class BaseDataEditor : UnityEditor.Editor
   {
       public override void OnInspectorGUI()
       {
           base.OnInspectorGUI();
           GameData baseData = (GameData)target;
           if (GUILayout.Button("Clear"))
           {
               PlayerPrefs.DeleteAll();
               baseData.ClearData();
           }
       }
   }
}