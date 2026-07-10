using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class ShopBundleItemView : MonoBehaviour
    {
        [SerializeField] protected Image _iconImage;
        [SerializeField] protected TMP_Text _labelText;

        public void SetIcon(Sprite sprite) => _iconImage.sprite = sprite;

        public void SetLabel(string text) => _labelText.text = text;
    }
}
