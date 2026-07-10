using UnityEngine;

namespace GameBrain.Casual
{
    [CreateAssetMenu(fileName = "GameMetaData", menuName = "GameBrain/Game Meta/New GameMetaData")]
    public class GameMetaData : ScriptableObject
    {
        public int HardLevelLoop;
    }
}
