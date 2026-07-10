using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace GameBrain.Casual
{
    public class StageUIItem : MonoBehaviour
    {
        [SerializeField] int id;
        [SerializeField] GameObject Bg;
        [SerializeField] Image activatedImage;
        [SerializeField] Image currentImage;
        
        public void Activate()
        {
            activatedImage.gameObject.SetActive(true);
            activatedImage.DOFade(1, 0.3f).SetEase(Ease.Linear);
            currentImage.gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            activatedImage.gameObject.SetActive(false);
            currentImage.gameObject.SetActive(false);
        }

        public void Complete()
        {
            currentImage.gameObject.SetActive(false);
        }
    }
}
