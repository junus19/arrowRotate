using UnityEngine;

namespace GameBrain.Casual
{
    [DisallowMultipleComponent, DefaultExecutionOrder(1)]
    public abstract class UIPanel : MonoBehaviour
    {
        protected float _scaleFactor = 1f;

        protected virtual void Awake()
        {
            if (!TryGetComponent(out Canvas canvas)) return;
            _scaleFactor = canvas.scaleFactor;
        }

        public virtual void OnInject(object[] args){}
        public virtual void Rebuild(){}
        
        public void SetActive(bool active) => gameObject.SetActive(active);
    }
}
