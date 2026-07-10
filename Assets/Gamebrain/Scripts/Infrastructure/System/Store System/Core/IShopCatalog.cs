using System.Collections.Generic;

namespace GameBrain.Store
{
    /// <summary>Read-only lookup of every purchasable item, keyed by id.</summary>
    public interface IShopCatalog
    {
        bool TryGet(string itemId, out IShopItem item);
        IReadOnlyCollection<IShopItem> Items { get; }
    }
}
