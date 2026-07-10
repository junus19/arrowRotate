using System;
using UnityEngine;

namespace GameBrain.Casual
{
    [Serializable]
    public class PreLoadBoardObject
    {
        [SerializeField] private bool _isBoardObjectActive;
        public bool IsBoardObjectActive => _isBoardObjectActive;

        [SerializeField] private BoardObjectType _boardObjectType;
        public BoardObjectType BoardObjectType => _boardObjectType;

        [SerializeField] private int _health = 3;
        public int Health => _health;
    }
}