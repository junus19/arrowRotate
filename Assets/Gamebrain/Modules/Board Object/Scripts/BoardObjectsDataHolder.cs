using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    [CreateAssetMenu(fileName = "New BoardObjectsDataHolder", menuName = "GameBrain/Game/Create BoardObjectsDataHolder")]
    public class BoardObjectsDataHolder : ScriptableObject
    {
        public List<BoardObjectInfo> BoardObjectInfos;
    }
}
