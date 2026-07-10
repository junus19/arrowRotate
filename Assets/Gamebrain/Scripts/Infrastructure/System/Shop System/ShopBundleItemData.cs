using UnityEngine;

namespace GameBrain.Casual
{
    public class ShopBundleItemData : ScriptableObject
    {
        [SerializeField] protected Sprite _icon;
        [SerializeField] protected string _label;
        [SerializeField] protected string _description;
        [SerializeField] protected float _price;

        public Sprite Icon => _icon;
        public string Label => _label;
        public string Description => _description;
        public float Price => _price;
    }
}
