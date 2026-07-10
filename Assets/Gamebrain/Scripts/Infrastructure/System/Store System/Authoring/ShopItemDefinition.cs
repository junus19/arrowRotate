using System.Collections.Generic;
using UnityEngine;

namespace GameBrain.Store
{
    /// <summary>How the store panel lays out this item.</summary>
    public enum StoreItemLayout
    {
        CoinTile,   // small tile in the coin grid (e.g. "1800 / EUR 16,99")
        Bundle,     // large multi-reward card shown in the top paged carousel (starter packs)
        Generic,    // single row (e.g. "No Ads")
        Hidden      // not shown in the panel (e.g. items only surfaced inside a popup/offer)
    }

    /// <summary>Optional ribbon shown on an item.</summary>
    public enum StoreBadgeType
    {
        None,
        BestValue,
        Popular,
        Custom
    }

    /// <summary>
    /// Designer-authored definition of a shop item. Holds presentation + economy data and builds an
    /// immutable runtime <see cref="IShopItem"/>. Rewards are polymorphic via [SerializeReference]:
    /// add any <see cref="IShopReward"/> (CurrencyReward, BoosterReward, …) from the inspector's + menu
    /// without changing this asset's type.
    /// </summary>
    [CreateAssetMenu(fileName = "New Shop Item", menuName = "GameBrain/Store/Shop Item")]
    public sealed class ShopItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _id;
        [SerializeField] private string _category;

        [Header("Presentation")]
        [SerializeField] private string _label;
        [SerializeField] private Sprite _icon;
        [SerializeField, TextArea] private string _description;

        [Header("Economy")]
        [SerializeField] private CurrencyType _currency;
        [SerializeField, Min(0)] private int _price;
        [Tooltip("Max times this item can be bought. -1 = unlimited.")]
        [SerializeField] private int _maxPurchases = -1;
        [SerializeField] private bool _isConsumable = true;

        [Header("Store UI / Presentation")]
        [SerializeField] private StoreItemLayout _layout = StoreItemLayout.CoinTile;
        [Tooltip("Shown instead of the numeric price for real-money items, e.g. \"EUR 2,29\".")]
        [SerializeField] private string _priceLabelOverride;
        [SerializeField] private StoreBadgeType _badge = StoreBadgeType.None;
        [SerializeField] private string _badgeCustomText;
        [Tooltip("Extra label on bundles, e.g. \"70% EXTRA\". Leave empty to hide.")]
        [SerializeField] private string _bonusLabel;
        [Tooltip("Countdown length for timed offers, in seconds. 0 = no timer.")]
        [SerializeField] private int _offerDurationSeconds;

        [Header("Rewards (granted on purchase)")]
        [SerializeReference] private List<IShopReward> _rewards = new List<IShopReward>();

        public string Id => _id;
        public string Category => _category;
        public string Label => _label;
        public Sprite Icon => _icon;
        public string Description => _description;
        public CurrencyType Currency => _currency;
        public int Price => _price;
        public int MaxPurchases => _maxPurchases;
        public bool IsConsumable => _isConsumable;
        public IReadOnlyList<IShopReward> Rewards => _rewards;

        // Presentation (consumed by the Store UI layer only).
        public StoreItemLayout Layout => _layout;
        public string PriceLabelOverride => _priceLabelOverride;
        public StoreBadgeType Badge => _badge;
        public string BadgeCustomText => _badgeCustomText;
        public string BonusLabel => _bonusLabel;
        public int OfferDurationSeconds => _offerDurationSeconds;

        /// <summary>Build the immutable runtime item the shop core consumes.</summary>
        public IShopItem ToRuntime() =>
            new ShopItem(_id, _currency, _price, _maxPurchases, _isConsumable, new List<IShopReward>(_rewards));
    }
}
