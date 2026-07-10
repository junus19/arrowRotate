using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class ShopItemPurchaseRequestEvent : IEvent
    {
        private readonly ShopItemData _data;

        public ShopItemData Data => _data;
        
        public ShopItemPurchaseRequestEvent(ShopItemData data)
        {
            _data = data;
        }
    }
}
