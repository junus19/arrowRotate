using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameBrain.Utils;
using Casual = GameBrain.Casual;

namespace GameBrain.Store
{
    /// <summary>
    /// "Buy 1, Get 1 Free" style offer popup. Renders the offer item's rewards into two columns, shows a
    /// price + countdown, and raises a purchase request for the offer's item id. Closes when that item is
    /// purchased. Wire the framework UIPopup's base close button to the "No Thanks" button in the prefab.
    /// </summary>
    public sealed class StoreOfferPopup : Casual.UIPopup
    {
        [Header("Offer")]
        [SerializeField] private Transform _leftColumn;
        [SerializeField] private Transform _rightColumn;
        [SerializeField] private StoreRewardEntryView _entryPrefab;
        [SerializeField] private TMP_Text _priceText;
        [SerializeField] private Button _buyButton;
        [SerializeField] private CountdownView _countdown;

        private ShopItemDefinition _definition;
        private StoreRewardIconSet _iconSet;
        private EventBinding<ShopPurchaseSucceededEvent> _succeededBinding;

        public void Bind(ShopItemDefinition definition, StoreRewardIconSet iconSet)
        {
            _definition = definition;
            _iconSet = iconSet;

            PopulateColumn(_leftColumn);
            PopulateColumn(_rightColumn);

            if (_priceText != null)
                _priceText.text = !string.IsNullOrEmpty(definition.PriceLabelOverride)
                    ? definition.PriceLabelOverride
                    : definition.Price.ToString();

            if (_countdown != null && definition.OfferDurationSeconds > 0)
                _countdown.StartCountdown(TimeSpan.FromSeconds(definition.OfferDurationSeconds));
        }

        private void PopulateColumn(Transform column)
        {
            if (column == null || _entryPrefab == null || _iconSet == null || _definition == null) return;

            for (int i = column.childCount - 1; i >= 0; i--)
                Destroy(column.GetChild(i).gameObject);

            var rewards = _definition.Rewards;
            for (int i = 0; i < rewards.Count; i++)
            {
                if (!_iconSet.TryResolve(rewards[i], out Sprite icon, out int amount)) continue;
                StoreRewardEntryView entry = Instantiate(_entryPrefab, column);
                entry.Set(icon, amount);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_buyButton != null) _buyButton.onClick.AddListener(OnBuyClicked);

            _succeededBinding = new EventBinding<ShopPurchaseSucceededEvent>(OnPurchaseSucceeded);
            EventBus<ShopPurchaseSucceededEvent>.Register(_succeededBinding);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_buyButton != null) _buyButton.onClick.RemoveListener(OnBuyClicked);

            EventBus<ShopPurchaseSucceededEvent>.Deregister(_succeededBinding);
        }

        private void OnBuyClicked()
        {
            if (_definition == null) return;
            EventBus<Casual.FxRequestEvent>.Raise(new Casual.FxRequestEvent(Casual.EffectType.Button));
            EventBus<ShopPurchaseRequestedEvent>.Raise(new ShopPurchaseRequestedEvent(_definition.Id));
        }

        private void OnPurchaseSucceeded(ShopPurchaseSucceededEvent evt)
        {
            if (_definition != null && evt.ItemId == _definition.Id) Close();
        }
    }
}
