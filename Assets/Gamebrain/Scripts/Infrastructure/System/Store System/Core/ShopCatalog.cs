using System.Collections.Generic;

namespace GameBrain.Store
{
    /// <summary>Dictionary-backed catalog with O(1) id lookup. Duplicate / empty ids are skipped, not thrown.</summary>
    public sealed class ShopCatalog : IShopCatalog
    {
        private readonly Dictionary<string, IShopItem> _itemsById = new Dictionary<string, IShopItem>();

        public IReadOnlyCollection<IShopItem> Items => _itemsById.Values;

        public ShopCatalog(IEnumerable<IShopItem> items, IShopLogger logger = null)
        {
            logger = logger ?? NullShopLogger.Instance;
            if (items == null) return;

            foreach (IShopItem item in items)
            {
                if (item == null) continue;

                if (string.IsNullOrEmpty(item.Id))
                {
                    logger.Warn("Skipping shop item with an empty id.");
                    continue;
                }

                if (_itemsById.ContainsKey(item.Id))
                {
                    logger.Warn($"Duplicate shop item id '{item.Id}' ignored.");
                    continue;
                }

                _itemsById.Add(item.Id, item);
            }
        }

        public bool TryGet(string itemId, out IShopItem item) =>
            _itemsById.TryGetValue(itemId ?? string.Empty, out item);
    }
}
