using System;
using UnityEngine;

namespace GameBrain.Casual
{
    [Serializable]
    public class ShopItemContent
    {
        [SerializeField] private ShopItemType _itemType;
        [ShowIf("_itemType", (int)ShopItemType.Booster)]
        [SerializeField] public BoosterType _boosterType;
        [SerializeField] private int _amount;

        public ShopItemType ItemType => _itemType;
        public int Amount => _amount;
        public BoosterType BoosterType => _boosterType;
    }
}
