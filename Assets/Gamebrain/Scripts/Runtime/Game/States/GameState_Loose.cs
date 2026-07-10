using UnityEngine;
using EC.Core.Common;
using GameBrain.Utils;
using UnityEngine.SceneManagement;

namespace GameBrain.Casual
{
    public class GameState_Loose : GameStateBase
    {
        private readonly EventBinding<MainMenuRequestedEvent> _mainMenuRequestEventBinding;

        public GameState_Loose(GameStateContext context) : base(context)
        {
            _mainMenuRequestEventBinding = new EventBinding<MainMenuRequestedEvent>(OnMainMenuRequested);
        }

        protected override void OnEnter(State previousState)
        {
            Debug.Log("On Enter Loose State!");
            EventBus<MainMenuRequestedEvent>.Register(_mainMenuRequestEventBinding);
            _guiService.LevelFailPanel.SetActive(true);
        }

        private void OnMainMenuRequested()
        {
            _stateMachine.ChangeState(_transitions[0].TargetState);
        }
        
        protected override void OnExit(State nextState)
        {
            EventBus<MainMenuRequestedEvent>.Deregister(_mainMenuRequestEventBinding);
            _levelManager.CurrentLevel.Unload();
            _guiService.LevelFailPanel.gameObject.SetActive(false);
            SceneManager.UnloadSceneAsync("Game");
            Debug.Log("On Exit Loose State!");
        }
    }
}
