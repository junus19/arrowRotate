using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameBrain.Utils;
using Casual = GameBrain.Casual;

namespace GameBrain.Store
{
    /// <summary>
    /// Base view for a single store item. Binds presentation from a <see cref="ShopItemDefinition"/>,
    /// raises <see cref="ShopPurchaseRequestedEvent"/> on tap, and reacts to the shop's outcome events.
    /// A plain MonoBehaviour like the framework's other view components (not a UIPanel).
    /// </summary>
    [DisallowMultipleComponent]
    public class StoreItemView : MonoBehaviour
    {
        [Header("Core")]
        [SerializeField] protected Image _iconImage;
        [SerializeField] protected TMP_Text _labelText;
        [SerializeField] protected Button _buyButton;

        [Header("Price")]
        [SerializeField] protected TMP_Text _priceText;
        [Tooltip("Coin/Gem icon next to the price. Hidden for real-money items.")]
        [SerializeField] protected Image _priceCurrencyIcon;

        [Header("Badge (optional)")]
        [SerializeField] protected GameObject _badgeRoot;
        [SerializeField] protected TMP_Text _badgeText;

        protected ShopItemDefinition _definition;
        protected StoreRewardIconSet _iconSet;

        private EventBinding<ShopPurchaseSucceededEvent> _succeededBinding;
        private EventBinding<ShopPurchaseFailedEvent> _failedBinding;

        public string ItemId => _definition != null ? _definition.Id : null;

        public virtual void Bind(ShopItemDefinition definition, StoreRewardIconSet iconSet)
        {
            _definition = definition;
            _iconSet = iconSet;

            if (_iconImage != null) _iconImage.sprite = definition.Icon;
            if (_labelText != null) _labelText.text = definition.Label;

            BindPrice();
            BindBadge();
        }

        protected virtual void BindPrice()
        {
            if (_priceText == null) return;

            bool realMoney = _definition.Currency == CurrencyType.RealMoney;
            if (realMoney && !string.IsNullOrEmpty(_definition.PriceLabelOverride))
            {
                _priceText.text = _definition.PriceLabelOverride;
                if (_priceCurrencyIcon != null) _priceCurrencyIcon.gameObject.SetActive(false);
            }
            else
            {
                _priceText.text = _definition.Price.ToString();
                if (_priceCurrencyIcon != null)
                {
                    Sprite icon = _iconSet != null ? _iconSet.GetCurrencyIcon(_definition.Currency) : null;
                    _priceCurrencyIcon.sprite = icon;
                    _priceCurrencyIcon.gameObject.SetActive(icon != null);
                }
            }
        }

        protected virtual void BindBadge()
        {
            if (_badgeRoot == null) return;

            if (_definition.Badge == StoreBadgeType.None)
            {
                _badgeRoot.SetActive(false);
                return;
            }

            _badgeRoot.SetActive(true);
            if (_badgeText != null) _badgeText.text = ResolveBadgeText(_definition);
        }

        protected static string ResolveBadgeText(ShopItemDefinition definition)
        {
            switch (definition.Badge)
            {
                case StoreBadgeType.BestValue: return "BEST VALUE";
                case StoreBadgeType.Popular: return "POPULAR";
                case StoreBadgeType.Custom: return definition.BadgeCustomText;
                default: return string.Empty;
            }
        }

        protected virtual void OnEnable()
        {
            if (_buyButton != null) _buyButton.onClick.AddListener(OnBuyClicked);

            _succeededBinding = new EventBinding<ShopPurchaseSucceededEvent>(OnPurchaseSucceeded);
            _failedBinding = new EventBinding<ShopPurchaseFailedEvent>(OnPurchaseFailed);
            EventBus<ShopPurchaseSucceededEvent>.Register(_succeededBinding);
            EventBus<ShopPurchaseFailedEvent>.Register(_failedBinding);
        }

        protected virtual void OnDisable()
        {
            if (_buyButton != null) _buyButton.onClick.RemoveListener(OnBuyClicked);

            EventBus<ShopPurchaseSucceededEvent>.Deregister(_succeededBinding);
            EventBus<ShopPurchaseFailedEvent>.Deregister(_failedBinding);
        }

        protected virtual void OnBuyClicked()
        {
            if (_definition == null) return;
            EventBus<Casual.FxRequestEvent>.Raise(new Casual.FxRequestEvent(Casual.EffectType.Button));
            EventBus<ShopPurchaseRequestedEvent>.Raise(new ShopPurchaseRequestedEvent(_definition.Id));
        }

        protected virtual void OnPurchaseSucceeded(ShopPurchaseSucceededEvent evt)
        {
            if (_definition == null || evt.ItemId != _definition.Id) return;
            OnPurchased();
        }

        protected virtual void OnPurchaseFailed(ShopPurchaseFailedEvent evt) { }

        /// <summary>Called when THIS item is purchased successfully. Override for owned / sold-out visuals.</summary>
        protected virtual void OnPurchased()
        {
            if (_definition != null && !_definition.IsConsumable && _buyButton != null)
                _buyButton.interactable = false;
        }
    }
}
