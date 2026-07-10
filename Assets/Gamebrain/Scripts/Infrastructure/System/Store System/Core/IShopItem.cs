using System.Collections.Generic;

namespace GameBrain.Store
{
    /// <summary>
    /// Marker for anything an item grants on purchase (coins, gems, boosters, lives, skins, …).
    /// Add a new reward kind by implementing this plus a matching <see cref="IRewardGranter"/> —
    /// no change to the shop core is required.
    /// </summary>
    public interface IShopReward { }

    /// <summary>Runtime, UI-agnostic description of a purchasable item.</summary>
    public interface IShopItem
    {
        string Id { get; }
        CurrencyType Currency { get; }
        int Price { get; }

        /// <summary>Maximum successful purchases allowed. Negative means unlimited.</summary>
        int MaxPurchases { get; }

        /// <summary>Consumables can be bought repeatedly; non-consumables only once.</summary>
        bool IsConsumable { get; }

        IReadOnlyList<IShopReward> Rewards { get; }
    }
}
