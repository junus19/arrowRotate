using System;
using UnityEngine;

namespace GameBrain.Casual
{
    [Serializable]
    public class Health
    {
        private int _current;
        private readonly int _maximum;

        public int Current => _current;
        public int Maximum => _maximum;
        
        public event Action OnChange;

        public Health(int maximum)
        {
            _current = maximum;
            _maximum = maximum;
        }
        
        public void Increase(int amount) => SetHealth(_current + amount);

        public void Decrease(int amount) => SetHealth(_current - amount);

        public void Restore() => SetHealth(_maximum);
        
        private void SetHealth(int health)
        {
            _current = Mathf.Clamp(health, 0, _maximum);
            OnChange?.Invoke();
        }
    }
}
