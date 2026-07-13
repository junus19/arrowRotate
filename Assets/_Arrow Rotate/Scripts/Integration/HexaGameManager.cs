using EC.Core.Common;
using GameBrain.Casual;
using UnityEngine;

namespace ArrowRotate.Integration
{
    /// <summary>
    /// Boot sahnesindeki GameManager'ın Hexa Arrows sürümü (Example/arrowJam deseni):
    /// gameplay state'i HexaGameState_Gameplay ile değiştirilir, transition'lar yeniden kurulur.
    /// Booster/BoardObject enjeksiyonu base'de kalır ama bu oyunda kullanılmaz.
    /// </summary>
    public class HexaGameManager : GameManager
    {
        protected override void OnInitializationCompleted(GameStateContext context)
        {
            base.OnInitializationCompleted(context);

            _inGameState = new HexaGameState_Gameplay(context, _boosterManager);

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

            Debug.Log("[Hexa] Gameplay state HexaGameState_Gameplay ile değiştirildi.");
        }
    }
}
