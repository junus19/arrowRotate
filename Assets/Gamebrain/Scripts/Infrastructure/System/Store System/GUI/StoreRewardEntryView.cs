using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GameBrain.Store
{
    /// <summary>A single reward chip inside a bundle/offer: an icon plus an "xN" amount.</summary>
    [DisallowMultipleComponent]
    public sealed class StoreRewardEntryView : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private TMP_Text _amountText;

        public void Set(Sprite icon, int amount)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
                _iconImage.enabled = icon != null;
            }
            if (_amountText != null) _amountText.text = $"x{amount}";
        }
    }
}
