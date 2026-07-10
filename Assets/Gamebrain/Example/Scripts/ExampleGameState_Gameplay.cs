using UnityEngine;
using EC.Core.Common;
using GameBrain.Utils;

namespace GameBrain.Casual.Example
{
    public class ExampleGameState_Gameplay : GameState_Gameplay
    {
        private ExampleTrigger _successTrigger;
        private ExampleTrigger _failTrigger;

        public ExampleGameState_Gameplay(GameStateContext context, BoosterManager boosterManager) : base(context, boosterManager, null)
        {
        }

        protected override void OnEnter(State previousState)
        {
            base.OnEnter(previousState);
            Debug.Log("OnEntered Example Gameplay state.");
        }

        protected override void OnLevelReady()
        {
            base.OnLevelReady();
            _successTrigger = GameObject.FindGameObjectWithTag("Success Trigger").GetComponent<ExampleTrigger>();
            _failTrigger = GameObject.FindGameObjectWithTag("Fail Trigger").GetComponent<ExampleTrigger>();
            _successTrigger.OnClick += OnSuccessCubeClicked;
            _failTrigger.OnClick += OnFailCubeClicked;
        }

        protected override void OnLevelFinished(Status status)
        {
            base.OnLevelFinished(status);
            _successTrigger.OnClick -= OnSuccessCubeClicked;
            _failTrigger.OnClick -= OnFailCubeClicked;
        }

        private void OnSuccessCubeClicked(ExampleTrigger trigger)
        {
            if (trigger.ClickCount == 5)
                OnLevelFinished(Status.Success);
            
            // Asks for position and color!
            EventBus<ScoreUpdateEvent>.Raise(new ScoreUpdateEvent(trigger.ClickCount, 100, trigger.transform.position, Color.green)); 
        }

        private void OnFailCubeClicked(ExampleTrigger trigger)
        {
            OnLevelFinished(Status.Fail);
        }
    }
}
