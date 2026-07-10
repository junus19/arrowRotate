using System;
using UnityEngine;

namespace GameBrain.Casual.Example
{
    public class ExampleTrigger : MonoBehaviour
    {
        private int _clickCount;

        public int ClickCount => _clickCount;
        
        public event Action<ExampleTrigger> OnClick;

        private void OnMouseDown()
        {
            _clickCount++;
            Debug.Log($"Trigger clicked for {_clickCount} times.", gameObject);
            OnClick?.Invoke(this);
        }
    }
}
