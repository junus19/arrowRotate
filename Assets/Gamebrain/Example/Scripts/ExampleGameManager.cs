using UnityEngine;
using EC.Core.Common;

namespace GameBrain.Casual.Example
{
    public class ExampleGameManager : GameManager
    {
        protected override void OnInitializationCompleted(GameStateContext context)
        {
            base.OnInitializationCompleted(context);
            
            _inGameState = new ExampleGameState_Gameplay(context, _boosterManager);
            
            _mainState.RemoveAllTransitions();
            _inGameState.RemoveAllTransitions();
            _winState.RemoveAllTransitions();
            _restartState.RemoveAllTransitions();
            _looseState.RemoveAllTransitions();
            
            _mainState.AddTransition(new Transition(_inGameState, null));
            _inGameState.AddTransition(new Transition(_looseState, null));
            _inGameState.AddTransition(new Transition(_winState, null));
            _inGameState.AddTransition(new Transition(_restartState, null));
            _inGameState.AddTransition(new Transition(_mainState, null));
            _looseState.AddTransition(new Transition(_restartState, null));
            _winState.AddTransition(new Transition(_mainState, null));
            _winState.AddTransition(new Transition(_inGameState, null));
            _restartState.AddTransition(new Transition(_inGameState, null));
            
            Debug.Log("Overriding states for example game.");
        }
    }
}
