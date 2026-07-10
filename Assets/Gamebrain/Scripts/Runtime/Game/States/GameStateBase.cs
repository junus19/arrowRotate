using EC.Core.Common;

namespace GameBrain.Casual
{
    public abstract class GameStateBase : State
    {
        protected readonly GameData _gameData;
        protected readonly GameConfig _gameConfig;
        protected readonly GUIService _guiService;
        protected readonly LevelManager _levelManager;
        protected readonly LevelData[] _levelData;
        protected readonly GameMode _gameMode;
        protected readonly LevelData _testLevel;
        
        protected GameStateBase(GameStateContext context) : base(context.StateMachine)
        {
            _gameData = context.GameData;
            _gameConfig = context.GameConfig;
            _guiService = context.GUIService;
            _levelManager = context.LevelManager;
            _levelData = context.GameConfig.Levels;
            _gameMode = context.GameConfig.GameMode;
            _testLevel = context.GameConfig.TestLevel;
        }

        protected abstract override void OnEnter(State previousState);

        protected abstract override void OnExit(State nextState);
    }
}
