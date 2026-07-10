using System;
using UnityEngine;

namespace GameBrain.Casual
{
    public interface ILevel
    {
        LevelData Data { get; }
        LevelStats Stats { get; }
        Transform Container { get; }
        Status Status { get; }
        
        event Action OnStart;
        event Action<Status> OnFinish;
        event Action OnRestart;

        Awaitable Load();
        Awaitable Unload();
        void Start();
        void Finish(Status status);
        void Restart();
    }
}
