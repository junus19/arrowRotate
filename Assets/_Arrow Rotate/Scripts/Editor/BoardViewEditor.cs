using ArrowRotate.View;
using UnityEditor;
using UnityEngine;

namespace ArrowRotate.EditorTools
{
    /// <summary>
    /// BoardView inspector'ına "Temayı Güncelle" butonu ekler — play modunda basınca
    /// aktif temanın gölge/kamera renklerini canlı taşlara uygular (arrowJam GridManagerEditor deseni).
    /// </summary>
    [CustomEditor(typeof(BoardView))]
    public class BoardViewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(6);
            var board = (BoardView)target;

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Temayı Güncelle (runtime)", GUILayout.Height(28)))
                    board.RefreshTheme();
            }
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Canlı güncelleme için Play modunda kullanın. " +
                    "Gölge + kamera rengi anında yansır; taş/buz renkleri için level yeniden yüklenmeli.",
                    MessageType.None);
        }
    }
}
