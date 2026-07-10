using System;
using UnityEngine;

namespace GameBrain.Casual
{
    [Serializable]
    public class BoardObjectInfo
    {
        [SerializeField] BoardObjectType boardObjectType;
        public BoardObjectType BoardObjectType => boardObjectType;

        [SerializeField] BoardObject boardObjectPrefab;
        public BoardObject BoardObjectPrefab => boardObjectPrefab;
    }
}