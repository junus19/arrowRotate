using UnityEngine;
using EC.Core.Common;
using UnityEngine.SceneManagement;

namespace GameBrain.Casual
{
    public class GameState_Restart : GameStateBase
    {
        private readonly Camera _mainCamera;
        
        public GameState_Restart(GameStateContext context) : base(context)
        {
            _mainCamera = context.MainCamera;
        }

        protected override void OnEnter(State previousState)
        {
            _levelManager.CurrentLevel.Unload();
            SceneManager.UnloadSceneAsync("Game");
            _stateMachine.ChangeState(_transitions[0].TargetState);
        }

        protected override void OnExit(State nextState)
        {
        }
    }
}
