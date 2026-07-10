using System.Linq;
using UnityEngine;
using EC.Core.Common;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class GameState_Main : GameStateBase
    {
        protected readonly EventBinding<PlayRequestedEvent> _playRequestEventBinding;
        protected readonly Camera _mainCamera;
        protected GameState_Gameplay _gameplayState;

        public GameState_Main(GameStateContext context) : base(context)
        {
            _mainCamera = context.MainCamera;
            _playRequestEventBinding = new EventBinding<PlayRequestedEvent>(OnPlayRequested);
        }

        protected override void OnEnter(State previousState)
        {
            Debug.Log("On Enter Main State!");
            
            _gameplayState ??= (GameState_Gameplay)_transitions.First(transition => transition.TargetState is GameState_Gameplay).TargetState;
            
            _mainCamera.gameObject.SetActive(true);
            _guiService.MainPanel.SetActive(true);
            _guiService.MainPanel.GetPanel<HomePanel>().SetLevelText(_gameData.GetVisualLevelIndex());//($"level {_gameData.GetVisualLevelIndex()}");
            _guiService.MainPanel.SetHardLevelTagActive(_levelManager.IsHardLevel(_gameData));
            EventBus<PlayRequestedEvent>.Register(_playRequestEventBinding);

            if(_gameData.GetLevelIndex() < _gameData.InstantStartLevelWithoutMainMenu)
                _stateMachine.ChangeState(_gameplayState);
         
        }

        protected virtual void OnPlayRequested(PlayRequestedEvent eventInfo)
        {
            _stateMachine.ChangeState(_gameplayState);
        }

        protected override void OnExit(State nextState)
        {
            EventBus<PlayRequestedEvent>.Deregister(_playRequestEventBinding);
            _guiService.MainPanel.SetActive(false);
            Debug.Log("On Exit Main State!");
        }
    }
}
