using UnityEngine;

namespace GameBrain.Casual
{
    [CreateAssetMenu(fileName = "New BoardObject Data", menuName = "GameBrain/Board Object Data")]
    public class BoardObjectData : ScriptableObject
    {
        [SerializeField] private BoardObjectType boardObjectType;
        public BoardObjectType BoardObjectType => boardObjectType;

        [SerializeField] private int boardObjectValue;
        public int BoardObjectValue => boardObjectValue;

        public BoardObject BoardObjectPrefab;
    }
}
