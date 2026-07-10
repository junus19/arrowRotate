using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    [CreateAssetMenu(fileName = "New Shop Category", menuName = "GameBrain/Shop/Shop Category")]
    public class ShopCategoryData : ScriptableObject
    {
        public string Id;
        public string Name;
        public Sprite Icon;
        public List<ShopItemData> Items = new List<ShopItemData>();
        public ShopCategoryView ViewPrefab;
    }
}
