using DG.Tweening;
using UnityEngine;

namespace GameBrain.Casual
{
    public class LoadingIndicator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        
        private void OnEnable()
        {
            _renderer.transform.DOLocalRotate(Vector3.right * 90 + Vector3.up * 360, 1.5f, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear);
        }

        private void OnDisable()
        {
            _renderer.transform.DOKill();
        }
    }
}
