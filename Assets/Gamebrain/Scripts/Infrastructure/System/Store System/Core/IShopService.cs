using System;

namespace GameBrain.Store
{
    /// <summary>
    /// Entry point for purchases. Implementations also listen for <see cref="ShopPurchaseRequestedEvent"/>
    /// on the EventBus so the UI stays fully decoupled. Dispose to unsubscribe.
    /// </summary>
    public interface IShopService : IDisposable
    {
        PurchaseOutcome Purchase(string itemId);

        /// <summary>Restore previously bought non-consumables (IAP). Publishes ShopPurchasesRestoredEvent.</summary>
        void RestorePurchases(Action<bool> onComplete = null);
    }
}
