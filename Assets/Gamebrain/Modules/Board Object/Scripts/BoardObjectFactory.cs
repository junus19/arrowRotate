using System;
using EC.Core.Common;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class BoardObjectFactory : IFactory<BoardObject>
    {
        private readonly List<BoardObjectInfo> _boardObjectDatas;

        public BoardObjectFactory(List<BoardObjectInfo> boardObjectDatas)
        {
            _boardObjectDatas = boardObjectDatas;
        }

        public BoardObject Create(object[] args = null)
        {
            BoardObjectInfo boardObjectData = _boardObjectDatas.Find(x => x.BoardObjectType == (BoardObjectType)args[0]);

            return UnityEngine.Object.Instantiate(boardObjectData.BoardObjectPrefab);
        }

        public Type GetItemType()
        {
            throw new NotImplementedException();
        }
    }
}
