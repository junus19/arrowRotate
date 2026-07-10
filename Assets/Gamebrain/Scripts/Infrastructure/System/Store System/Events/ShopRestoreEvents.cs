using GameBrain.Utils;

namespace GameBrain.Store
{
    /// <summary>UI → service. Requests restoring previously bought non-consumables (IAP).</summary>
    public sealed class ShopRestoreRequestedEvent : IEvent { }

    /// <summary>Service → UI. Restore finished; Success reflects the platform restore call.</summary>
    public sealed class ShopPurchasesRestoredEvent : IEvent
    {
        public bool Success { get; }
        public ShopPurchasesRestoredEvent(bool success) => Success = success;
    }
}
