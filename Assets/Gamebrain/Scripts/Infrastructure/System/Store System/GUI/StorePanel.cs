using UnityEngine;
using GameBrain.Utils;
using Casual = GameBrain.Casual;

namespace GameBrain.Store
{
    /// <summary>
    /// Root store panel. Builds item views from a <see cref="ShopCatalogDefinition"/> into the right
    /// section by layout, and shows a failure popup driven by <see cref="ShopPurchaseFailedEvent"/>.
    /// Compatible with the framework's UIPanel — toggle it with SetActive like the legacy ShopPanel, or
    /// place it as a page under a navigation bar.
    /// </summary>
    public sealed class StorePanel : Casual.UIPanel
    {
        [Header("Data")]
        [SerializeField] private ShopCatalogDefinition _catalog;
        [SerializeField] private StoreRewardIconSet _iconSet;

        [Header("Containers")]
        [Tooltip("Horizontal paged carousel of bundles / starter packs.")]
        [SerializeField] private StoreCarouselView _bundleCarousel;
        [Tooltip("Vertical list of single rows (e.g. No Ads).")]
        [SerializeField] private Transform _genericContainer;
        [Tooltip("Grid of coin tiles.")]
        [SerializeField] private Transform _coinGridContainer;

        [Header("View Prefabs")]
        [SerializeField] private StoreBundleView _bundlePrefab;
        [SerializeField] private StoreItemView _genericItemPrefab;
        [SerializeField] private StoreItemView _coinItemPrefab;

        [Header("Popups")]
        [SerializeField] private StoreMessagePopup _messagePopup;

        private EventBinding<ShopPurchaseFailedEvent> _failedBinding;
        private bool _built;

        protected override void Awake()
        {
            base.Awake();
            Build();
        }

        private void Build()
        {
            if (_built || _catalog == null) return;
            _built = true;

            int bundleCount = 0;
            foreach (ShopItemDefinition definition in _catalog.Items)
            {
                if (definition == null) continue;
                if (CreateView(definition) == StoreItemLayout.Bundle) bundleCount++;
            }

            if (_bundleCarousel != null) _bundleCarousel.Initialize(bundleCount);
        }

        private StoreItemLayout CreateView(ShopItemDefinition definition)
        {
            switch (definition.Layout)
            {
                case StoreItemLayout.CoinTile:
                    if (_coinItemPrefab != null && _coinGridContainer != null)
                        Instantiate(_coinItemPrefab, _coinGridContainer).Bind(definition, _iconSet);
                    break;

                case StoreItemLayout.Bundle:
                    if (_bundlePrefab != null && _bundleCarousel != null && _bundleCarousel.Content != null)
                        Instantiate(_bundlePrefab, _bundleCarousel.Content).Bind(definition, _iconSet);
                    break;

                case StoreItemLayout.Generic:
                    if (_genericItemPrefab != null && _genericContainer != null)
                        Instantiate(_genericItemPrefab, _genericContainer).Bind(definition, _iconSet);
                    break;

                case StoreItemLayout.Hidden:
                    // Not shown in the panel (e.g. offer items surfaced only via a popup).
                    break;
            }

            return definition.Layout;
        }

        private void OnEnable()
        {
            _failedBinding = new EventBinding<ShopPurchaseFailedEvent>(OnPurchaseFailed);
            EventBus<ShopPurchaseFailedEvent>.Register(_failedBinding);
            if (_messagePopup != null) _messagePopup.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            EventBus<ShopPurchaseFailedEvent>.Deregister(_failedBinding);
        }

        private void OnPurchaseFailed(ShopPurchaseFailedEvent evt)
        {
            if (_messagePopup != null) _messagePopup.ShowFailure(evt.Reason);
        }
    }
}
