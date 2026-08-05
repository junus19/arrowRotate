using UnityEngine;
using System.Linq;
using EC.Core.Common;
using GameBrain.SDK;
using GameBrain.Store;
using GameBrain.Utils;
using UnityEngine.SceneManagement;

namespace GameBrain.Casual
{
    public class GameState_Win : GameStateBase
    {
        private readonly EventBinding<MainMenuRequestedEvent> _mainMenuRequestEventBinding;
        private readonly EventBinding<NextLevelRequestedEvent> _nextLevelRequestEventBinding;
        private readonly EventBinding<DoubleRewardRequestedEvent> _doubleRewardRequestEventBinding;
        private readonly Camera _mainCamera;
        protected readonly CurrencyManager _currencyManager;
        protected readonly AnalyticManager  _analyticManager;

        public GameState_Win(GameStateContext context, CurrencyManager currencyManager, AnalyticManager analyticManager) : base(context)
        {
            _mainCamera = context.MainCamera;
            _mainMenuRequestEventBinding = new EventBinding<MainMenuRequestedEvent>(OnMainMenuRequested);
            _nextLevelRequestEventBinding = new EventBinding<NextLevelRequestedEvent>(OnNextLevelRequested);
            _doubleRewardRequestEventBinding = new EventBinding<DoubleRewardRequestedEvent>(OnDoubleRewardRequested);
            _currencyManager = currencyManager;
            _analyticManager = analyticManager;
        }

        protected override void OnEnter(State previousState)
        {
            EventBus<MainMenuRequestedEvent>.Register(_mainMenuRequestEventBinding);
            EventBus<NextLevelRequestedEvent>.Register(_nextLevelRequestEventBinding);
            EventBus<DoubleRewardRequestedEvent>.Register(_doubleRewardRequestEventBinding);
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
                _analyticManager?.AnalyticsService.SendResourceEvent(ResourceFlowType.Gain, "Coin",_gameData.LevelCompleteReward, "Coin", "level_complete_reward");
                _stateMachine.ChangeState(_transitions.First(state => state.TargetState is GameState_Main).TargetState);
            }
            else
                OnNextLevelRequested();
        }

        private void OnNextLevelRequested()
        {
            _currencyManager.Deposit(CurrencyType.Coin, _gameData.LevelCompleteReward);
            _analyticManager?.AnalyticsService.SendResourceEvent(ResourceFlowType.Gain, "Coin",_gameData.LevelCompleteReward, "Coin", "level_complete_reward");
            _stateMachine.ChangeState(_transitions.First(state => state.TargetState is GameState_Gameplay).TargetState);
        }
        
        private void OnDoubleRewardRequested()
        {
            _analyticManager?.AnalyticsService.SendAdClickEvent("DoubleReward_Rewarded", "applovin_max");
            _analyticManager?.ADService.ShowRewardedAd("DoubleReward_Rewarded", OnDoubleRewardClaimed);
        }

        private void OnDoubleRewardClaimed()
        {
            _currencyManager.Deposit(CurrencyType.Coin, _gameData.LevelCompleteReward * 2);
            _analyticManager?.AnalyticsService.SendAdImpressionEvent(AdType.Rewarded, "DoubleReward_Rewarded", "applovin_max");
            _analyticManager?.AnalyticsService.SendResourceEvent(ResourceFlowType.Gain, "Coin",_gameData.LevelCompleteReward * 2f, "Coin", "level_complete_reward_double");
            
            if (_gameData.GetLevelIndex() > 3)
                _stateMachine.ChangeState(_transitions.First(state => state.TargetState is GameState_Main).TargetState);
            else
                _stateMachine.ChangeState(_transitions.First(state => state.TargetState is GameState_Gameplay).TargetState);
        }

        protected override void OnExit(State nextState)
        {
            EventBus<MainMenuRequestedEvent>.Deregister(_mainMenuRequestEventBinding);
            EventBus<NextLevelRequestedEvent>.Deregister(_nextLevelRequestEventBinding);
            EventBus<DoubleRewardRequestedEvent>.Deregister(_doubleRewardRequestEventBinding);
            SceneManager.UnloadSceneAsync("Game");
            _levelManager.CurrentLevel.Unload();
            _guiService.LevelCompletePanel.gameObject.SetActive(false);
            //_guiService.TopPanel.SetActive(false);

            _mainCamera.gameObject.SetActive(true);
        }
    }
}
