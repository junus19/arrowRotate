using System;

namespace GameBrain.Store
{
    /// <summary>
    /// Platform in-app-purchase seam. Real IAP is asynchronous, so a purchase completes via the
    /// onComplete callback (true = purchased, false = failed/cancelled). See <see cref="UgsIapService"/>.
    /// </summary>
    public interface IIapService
    {
        bool IsAvailable(string productId);

        /// <summary>Start a purchase; the result arrives later on onComplete (main thread).</summary>
        void Purchase(string productId, Action<bool> onComplete);

        /// <summary>Restore previously bought non-consumables (Apple); Google restores on init.</summary>
        void RestorePurchases(Action<bool> onComplete);

        /// <summary>Raised per owned/restored product id (from a restore or an out-of-band purchase).</summary>
        event Action<string> PurchaseRestored;
    }

    /// <summary>Deterministic test/demo IAP. Never contacts a store or charges real money.</summary>
    public sealed class StubIapService : IIapService
    {
        private readonly bool _succeeds;
        public StubIapService(bool succeeds = true) => _succeeds = succeeds;
        public bool IsAvailable(string productId) => true;
        public void Purchase(string productId, Action<bool> onComplete) => onComplete?.Invoke(_succeeds);
        public void RestorePurchases(Action<bool> onComplete) => onComplete?.Invoke(true);
        public event Action<string> PurchaseRestored;
    }

    /// <summary>
    /// Routes RealMoney purchases through an async <see cref="IIapService"/> (item id = product id).
    /// Implements <see cref="IAsyncPaymentProcessor"/> (purchase) and <see cref="IRestorablePaymentProcessor"/>
    /// (restore); the synchronous <see cref="IPaymentProcessor.Pay"/> is never used for this currency.
    /// </summary>
    public sealed class RealMoneyPaymentProcessor : IPaymentProcessor, IAsyncPaymentProcessor, IRestorablePaymentProcessor
    {
        private readonly IIapService _iap;
        private readonly IShopLogger _logger;

        public CurrencyType Currency => CurrencyType.RealMoney;

        public RealMoneyPaymentProcessor(IIapService iap, IShopLogger logger = null)
        {
            _iap = iap;
            _logger = logger ?? NullShopLogger.Instance;
        }

        public bool CanPay(IShopItem item) => _iap != null && _iap.IsAvailable(item.Id);

        // Synchronous Pay is not applicable to async IAP; the shop uses BeginPay instead.
        public bool Pay(IShopItem item)
        {
            _logger.Warn($"RealMoneyPaymentProcessor.Pay called synchronously for '{item.Id}'; use the async path.");
            return false;
        }

        public void BeginPay(IShopItem item, Action<bool> onComplete)
        {
            if (_iap == null) { onComplete?.Invoke(false); return; }
            _iap.Purchase(item.Id, success =>
            {
                if (!success) _logger.Warn($"IAP purchase failed or was cancelled for product '{item.Id}'.");
                onComplete?.Invoke(success);
            });
        }

        public void Restore(Action<bool> onComplete)
        {
            if (_iap == null) { onComplete?.Invoke(false); return; }
            _iap.RestorePurchases(onComplete);
        }

        // Forwards the IAP service's restored-product notifications upward.
        public event Action<string> ProductRestored
        {
            add { if (_iap != null) _iap.PurchaseRestored += value; }
            remove { if (_iap != null) _iap.PurchaseRestored -= value; }
        }
    }
}
