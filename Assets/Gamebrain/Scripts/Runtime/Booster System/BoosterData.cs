using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    [CreateAssetMenu(fileName = "NewBoosterData", menuName = "GameBrain/BoosterData")]
    public class BoosterData : ScriptableObject
    {
        public List<BoosterItemData> BoosterDatas;
    }
}
