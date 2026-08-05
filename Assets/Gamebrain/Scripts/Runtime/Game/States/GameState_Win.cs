using UnityEngine;
using System.Linq;
using EC.Core.Common;
using GameBrain.Store;
using GameBrain.Utils;
using UnityEngine.SceneManagement;

namespace GameBrain.Casual
{
    public class GameState_Win : GameStateBase
    {
        private readonly EventBinding<MainMenuRequestedEvent> _mainMenuRequestEventBinding;
        private readonly EventBinding<NextLevelRequestedEvent> _nextLevelRequestEventBinding;
        private readonly Camera _mainCamera;
        protected readonly CurrencyManager _currencyManager; 

        public GameState_Win(GameStateContext context, CurrencyManager currencyManager) : base(context)
        {
            _mainCamera = context.MainCamera;
            _mainMenuRequestEventBinding = new EventBinding<MainMenuRequestedEvent>(OnMainMenuRequested);
            _nextLevelRequestEventBinding = new EventBinding<NextLevelRequestedEvent>(OnNextLevelRequested);
            _currencyManager = currencyManager;
        }

        protected override void OnEnter(State previousState)
        {
            EventBus<MainMenuRequestedEvent>.Register(_mainMenuRequestEventBinding);
            EventBus<NextLevelRequestedEvent>.Register(_nextLevelRequestEventBinding);
            _guiService.LevelCompletePanel.gameObject.SetActive(true);
            _guiService.TopPanel.SetActive(true);
            _guiService.TopPanel.SetStatusOfTopBarButtons(false, false);

            _levelManager.LevelCompleted(_gameData);
        }

        private void OnMainMenuRequested()
        {
            if (_gameData.GetLevelIndex() > 3)
            {
                _currencyManager.Deposit(CurrencyType.Coin, _gameData.LevelCompleteReward);
                _stateMachine.ChangeState(_transitions.First(state => state.TargetState is GameState_Main).TargetState);
            }
            else
                OnNextLevelRequested();
        }

        private void OnNextLevelRequested()
        {
            _currencyManager.Deposit(CurrencyType.Coin, _gameData.LevelCompleteReward);
            _stateMachine.ChangeState(_transitions.First(state => state.TargetState is GameState_Gameplay).TargetState);
        }

        protected override void OnExit(State nextState)
        {
            EventBus<MainMenuRequestedEvent>.Deregister(_mainMenuRequestEventBinding);
            EventBus<NextLevelRequestedEvent>.Deregister(_nextLevelRequestEventBinding);
            SceneManager.UnloadSceneAsync("Game");
            _levelManager.CurrentLevel.Unload();
            _guiService.LevelCompletePanel.gameObject.SetActive(false);
            //_guiService.TopPanel.SetActive(false);

            _mainCamera.gameObject.SetActive(true);
        }
    }
}
