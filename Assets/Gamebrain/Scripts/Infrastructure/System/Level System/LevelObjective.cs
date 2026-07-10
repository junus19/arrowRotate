using System;
using UnityEngine;

namespace GameBrain.Casual
{
    public abstract class LevelObjective : ScriptableObject
    {
        private Status _status = Status.NotCompleted;
        public Status Status => _status;
        
        public event Action OnAchieved;
        public event Action OnFailed;
        
        public void MarkAchieved()
        {
            _status = Status.Success;
            OnAchieved?.Invoke();
        }
        
        public void MarkFailed()
        {
            _status = Status.Fail;
            OnFailed?.Invoke();
        }

        public void Reset()
        {
            _status = Status.NotCompleted;
        }
    }
}
