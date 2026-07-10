using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class LoadingIndicatorUI : MonoBehaviour
    {
        [SerializeField] private Image _image;
        
        private void OnEnable()
        {
            _image.transform.DOLocalRotate(Vector3.back * 360, 1.5f, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear);
        }

        private void OnDisable()
        {
            _image.transform.DOKill();
        }
    }
}
