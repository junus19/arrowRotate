using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class ShopItemTransactionDeclinedEvent : IEvent
    {
        private readonly ShopItemData _data;
        private readonly string _message;

        public ShopItemData Data => _data;
        public string Message => _message;

        public ShopItemTransactionDeclinedEvent(ShopItemData data, string declineMessage)
        {
            _data = data;
            _message = declineMessage;
        }
    }
}
