using UnityEngine;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class BoardObjectBrokenEvent : IEvent
    {
        private readonly BoardObjectType _boardObjectType;
        public BoardObjectType BoardObjectType => _boardObjectType;

        public Vector3 boardObjectPosition;

        public BoardObjectBrokenEvent(BoardObjectType boardObjectType, Vector3 _boardObjectPosition)
        {
            _boardObjectType = boardObjectType;
            boardObjectPosition = _boardObjectPosition;
        }
    }
}
