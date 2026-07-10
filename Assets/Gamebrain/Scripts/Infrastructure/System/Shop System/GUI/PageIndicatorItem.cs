using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class PageIndicatorItem : MonoBehaviour
    {
        [SerializeField] protected Image _selectedImage;
        
        public void SetSelected(bool selected) => _selectedImage.gameObject.SetActive(selected); 
    }
}
