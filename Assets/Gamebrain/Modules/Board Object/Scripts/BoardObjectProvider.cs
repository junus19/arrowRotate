using UnityEngine;

namespace GameBrain.Casual
{
    public class BoardObjectProvider
    {
        private readonly BoardObjectFactory _boardObjectFactory;

        public BoardObjectProvider(BoardObjectFactory boardObjectFactory)
        {
            _boardObjectFactory = boardObjectFactory;
        }

        public void RequestBoardObject(ICell cell, BoardObjectType boardObjectType, int objValue, Transform parent = null)
        {
            BoardObject boardObject = _boardObjectFactory.Create(new object[1] { boardObjectType });
            boardObject.LoadBoardObject(cell, objValue);
            // if (parent != null)
            //     boardObject.transform.SetParent(parent);
        }
    }
}
