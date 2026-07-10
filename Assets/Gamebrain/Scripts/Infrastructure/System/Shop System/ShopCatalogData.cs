using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    [CreateAssetMenu(fileName = "New Shop Catalog", menuName = "GameBrain/Shop/Shop Catalog")]
    public class ShopCatalogData : ScriptableObject
    {
        [SerializeField] private List<ShopCategoryData> _categories = new List<ShopCategoryData>();

        public List<ShopCategoryData> Categories => _categories;
    }
}
