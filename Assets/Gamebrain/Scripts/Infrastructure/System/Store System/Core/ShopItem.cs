using System.Collections.Generic;

namespace GameBrain.Store
{
    /// <summary>Plain runtime implementation of <see cref="IShopItem"/> (no Unity dependency, fully testable).</summary>
    public sealed class ShopItem : IShopItem
    {
        public string Id { get; }
        public CurrencyType Currency { get; }
        public int Price { get; }
        public int MaxPurchases { get; }
        public bool IsConsumable { get; }
        public IReadOnlyList<IShopReward> Rewards { get; }

        public ShopItem(string id, CurrencyType currency, int price, int maxPurchases, bool isConsumable,
            IReadOnlyList<IShopReward> rewards)
        {
            Id = id;
            Currency = currency;
            Price = price;
            MaxPurchases = maxPurchases;
            IsConsumable = isConsumable;
            Rewards = rewards ?? new List<IShopReward>();
        }
    }
}
