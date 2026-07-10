using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class ShopCategoryView : MonoBehaviour
    {
        [SerializeField] protected Transform _contentContainer;
        [SerializeField] protected List<ShopItemView> _views;

        public List<ShopItemView> Views => _views;
        
        public virtual void Init(ShopCategoryData data)
        {
            _views =  new List<ShopItemView>();
            foreach (ShopItemData shopItemData in data.Items)
            {
                ShopItemView view = Instantiate(shopItemData.ViewPrefab, _contentContainer);
                view.Init(shopItemData);
                _views.Add(view);
            }
        }
    }
}
