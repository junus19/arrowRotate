using System;

namespace GameBrain.Store
{
    /// <summary>A payment processor that can restore previously bought non-consumables (e.g. IAP).</summary>
    public interface IRestorablePaymentProcessor
    {
        /// <summary>Trigger a platform restore; onComplete reports whether the restore call succeeded.</summary>
        void Restore(Action<bool> onComplete);

        /// <summary>Raised per owned/restored product id, so the shop can re-grant the entitlement.</summary>
        event Action<string> ProductRestored;
    }
}
