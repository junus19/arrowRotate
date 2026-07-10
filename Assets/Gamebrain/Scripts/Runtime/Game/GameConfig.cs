using System;
using UnityEngine;

namespace GameBrain.Casual
{
    [CreateAssetMenu(menuName = "GameBrain/Game Config", fileName = "New Game Config", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [SerializeField] protected GameMode _gameMode;
        
        [Header("Level")]
        [SerializeField] protected LevelData[] _levels; 
        [ShowIf("_gameMode", GameMode.Test, "Test Config")]
        [SerializeField] protected LevelData _testLevel;

        public GameMode GameMode => _gameMode;
        public LevelData[] Levels => _levels;
        public LevelData TestLevel => _testLevel;

        public event Action OnUpdate;
    }
}
