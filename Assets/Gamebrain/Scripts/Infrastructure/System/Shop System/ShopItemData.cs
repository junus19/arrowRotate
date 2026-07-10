using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    [CreateAssetMenu(fileName = "New Shop Item Data", menuName = "GameBrain/Shop/Shop Item")]
    public class ShopItemData : ScriptableObject
    {
        [Header("Config")]
        [SerializeField] protected string _id;
        [SerializeField] protected ShopItemType _type;
        [SerializeField] protected Sprite _icon;
        [SerializeField] protected string _label;
        [SerializeField] protected string _description;
        [SerializeField] protected bool _isConsumable;
        
        [Header("Purchase")]
        [SerializeField] protected float _price;
        [Tooltip("Maximum purchase amount. -1 means infinite.")]
        [SerializeField] protected int _maximumPurchase = -1;
        [SerializeField] protected CurrencyType _currencyType;
        
        [Header("On Purchase")]
        [SerializeField] protected List<ShopItemContent> _contents = new List<ShopItemContent>();
        
        [Header("UI")]
        [SerializeField] protected ShopItemView _viewPrefab;

        public string Id => _id;
        public ShopItemType Type => _type;
        public Sprite Icon => _icon;
        public string Label => _label;
        public string Description => _description;
        public bool IsConsumable => _isConsumable;
        public float Price => _price;
        public int MaximumPurchase => _maximumPurchase;
        public ShopItemView ViewPrefab => _viewPrefab;
        public CurrencyType CurrencyType => _currencyType;
        public List<ShopItemContent> Contents => _contents;
    }
}
