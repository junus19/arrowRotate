using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    [DisallowMultipleComponent]
    public class ShopItemView : MonoBehaviour
    {
        [SerializeField] protected Image _iconImage;
        [SerializeField] protected TMP_Text _labelText;
        [SerializeField] protected TMP_Text _priceText;
        [SerializeField] protected Button _buyButton;
        protected ShopItemData _data;

        public virtual void Init(ShopItemData data)
        {
            _data = data;
            SetIcon(data.Icon);
            SetLabel(data.Label);
            SetPrice(data.Price);
        }

        protected virtual void OnEnable()
        {
            _buyButton.onClick.AddListener(OnBuyButtonClicked);
        }

        protected virtual void OnDisable()
        {
            _buyButton.onClick.RemoveListener(OnBuyButtonClicked);
        }

        protected virtual void OnBuyButtonClicked()
        {
            EventBus<ShopItemPurchaseRequestEvent>.Raise(new ShopItemPurchaseRequestEvent(_data));
        }

        public void SetIcon(Sprite sprite)
        {
            _iconImage.sprite = sprite;
        }

        public void SetLabel(string text)
        {
            _labelText.text = text;
        }

        public void SetPrice(float price)
        {
            _priceText.text = _data.CurrencyType == CurrencyType.RealMoney ? $"${price}" : $"{price}"; 
        }
    }
}
