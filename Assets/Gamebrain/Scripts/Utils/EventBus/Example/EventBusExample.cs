using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameBrain.Utils
{
    public struct GameStartInfo : IEvent
    {

    }

    public class GameStartButton 
    {
        private void OnStartButtonClicked()
        {
            EventBus<GameStartInfo>.Raise(new GameStartInfo());
        }
    }

    public class GameStarter
    {
        private EventBinding<GameStartInfo> gameStartButtonEvent;

        public void Init()
        {
            gameStartButtonEvent = new EventBinding<GameStartInfo>(OnGameStartRequested);
            EventBus<GameStartInfo>.Register(gameStartButtonEvent);
        }

        private void OnGameStartRequested(GameStartInfo start)
        {
            Debug.Log("Game");
        }
    }
}
