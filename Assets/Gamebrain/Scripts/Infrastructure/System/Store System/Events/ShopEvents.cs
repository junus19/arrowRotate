using GameBrain.Utils;

namespace GameBrain.Store
{
    /// <summary>UI → service. Asks the shop to attempt a purchase. Carries only the item id.</summary>
    public sealed class ShopPurchaseRequestedEvent : IEvent
    {
        public string ItemId { get; }
        public ShopPurchaseRequestedEvent(string itemId) => ItemId = itemId;
    }

    /// <summary>Service → listeners. A purchase completed successfully.</summary>
    public sealed class ShopPurchaseSucceededEvent : IEvent
    {
        public IShopItem Item { get; }
        public string ItemId => Item != null ? Item.Id : null;
        public ShopPurchaseSucceededEvent(IShopItem item) => Item = item;
    }

    /// <summary>Service → listeners. A purchase was declined. UI maps <see cref="Reason"/> to text.</summary>
    public sealed class ShopPurchaseFailedEvent : IEvent
    {
        public string ItemId { get; }
        public PurchaseFailureReason Reason { get; }

        public ShopPurchaseFailedEvent(string itemId, PurchaseFailureReason reason)
        {
            ItemId = itemId;
            Reason = reason;
        }
    }
}
