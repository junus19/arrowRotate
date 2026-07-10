using System;

namespace GameBrain.Casual
{
    [Serializable]
    public class LevelStats
    {
        public event Action OnChanged;
        
        public virtual void Reset(){}
        
        protected virtual void OnStatsChanged()
        {
            OnChanged?.Invoke();
        }
    }
}
