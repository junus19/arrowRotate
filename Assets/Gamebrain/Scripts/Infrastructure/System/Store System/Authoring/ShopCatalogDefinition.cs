using System.Collections.Generic;
using UnityEngine;

namespace GameBrain.Store
{
    /// <summary>Designer-authored set of items. Builds a runtime <see cref="IShopCatalog"/>.</summary>
    [CreateAssetMenu(fileName = "New Shop Catalog", menuName = "GameBrain/Store/Shop Catalog")]
    public sealed class ShopCatalogDefinition : ScriptableObject
    {
        [SerializeField] private List<ShopItemDefinition> _items = new List<ShopItemDefinition>();

        public IReadOnlyList<ShopItemDefinition> Items => _items;

        public IShopCatalog BuildCatalog(IShopLogger logger = null)
        {
            List<IShopItem> runtimeItems = new List<IShopItem>(_items.Count);
            for (int i = 0; i < _items.Count; i++)
            {
                ShopItemDefinition definition = _items[i];
                if (definition != null) runtimeItems.Add(definition.ToRuntime());
            }
            return new ShopCatalog(runtimeItems, logger);
        }
    }
}
