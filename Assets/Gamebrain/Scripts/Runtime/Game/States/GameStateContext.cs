using UnityEngine;
using GameBrain.SDK;
using EC.Core.Common;

namespace GameBrain.Casual
{
    public class GameStateContext : StateContext
    {
        protected readonly GameData _gameData;
        protected readonly GameConfig _gameConfig;

        protected readonly GUIService _guiService;
        protected readonly LevelManager _levelManager;
        protected readonly AnalyticManager _analyticManager;
        protected readonly GameMetaSystem _gameMetaSystem;

        protected readonly Camera _mainCamera;

        public GameData GameData => _gameData;
        public GameConfig GameConfig => _gameConfig;
        public GUIService GUIService => _guiService;
        public LevelManager LevelManager => _levelManager;
        public AnalyticManager AnalyticManager => _analyticManager;
        public GameMetaSystem GameMetaSystem => _gameMetaSystem;
        public Camera MainCamera => _mainCamera;

        public GameStateContext
        (
            StateMachine stateMachine,
            GameData gameData,
            GameConfig gameConfig,
            GUIService guiService,
            LevelManager levelManager,
            AnalyticManager analyticManager,
            GameMetaSystem gameMetaSystem,
            Camera mainCamera
        ) : base(stateMachine)
        {
            _gameData = gameData;
            _gameConfig = gameConfig;
            _guiService = guiService;
            _levelManager = levelManager;
            _analyticManager = analyticManager;
            _gameMetaSystem = gameMetaSystem;
            _mainCamera = mainCamera;
        }
    }
}
