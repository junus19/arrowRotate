using System;
using System.Collections.Generic;
using GameBrain.Utils;

namespace GameBrain.Store
{
    /// <summary>
    /// Orchestrates a purchase: validate → check capability → charge → grant → record → publish.
    /// Every collaborator is injected as an abstraction, so behaviour is composed rather than hard-coded
    /// (DIP). The flow is atomic in intent: it never charges without being able to deliver, and never
    /// delivers without charging.
    /// </summary>
    public sealed class ShopService : IShopService
    {
        private readonly IShopCatalog _catalog;
        private readonly IReadOnlyList<IPurchaseValidator> _validators;
        private readonly Dictionary<CurrencyType, IPaymentProcessor> _processors;
        private readonly Dictionary<Type, IRewardGranter> _granters;
        private readonly IPurchaseHistory _history;
        private readonly IShopLogger _logger;
        private readonly ShopContext _context;
        private readonly EventBinding<ShopPurchaseRequestedEvent> _purchaseRequestedBinding;
        private readonly EventBinding<ShopRestoreRequestedEvent> _restoreRequestedBinding;
        private readonly List<IRestorablePaymentProcessor> _restorables = new List<IRestorablePaymentProcessor>();

        public ShopService(
            IShopCatalog catalog,
            IEnumerable<IPaymentProcessor> paymentProcessors,
            IEnumerable<IRewardGranter> rewardGranters,
            IEnumerable<IPurchaseValidator> validators = null,
            IPurchaseHistory history = null,
            IShopLogger logger = null)
        {
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _logger = logger ?? NullShopLogger.Instance;
            _history = history ?? new InMemoryPurchaseHistory();
            _context = new ShopContext(_history);
            _validators = validators != null
                ? new List<IPurchaseValidator>(validators)
                : new List<IPurchaseValidator>();

            _processors = new Dictionary<CurrencyType, IPaymentProcessor>();
            if (paymentProcessors != null)
            {
                foreach (IPaymentProcessor processor in paymentProcessors)
                {
                    if (processor != null) _processors[processor.Currency] = processor;
                }
            }

            _granters = new Dictionary<Type, IRewardGranter>();
            if (rewardGranters != null)
            {
                foreach (IRewardGranter granter in rewardGranters)
                {
                    if (granter != null && granter.RewardType != null) _granters[granter.RewardType] = granter;
                }
            }

            _purchaseRequestedBinding = new EventBinding<ShopPurchaseRequestedEvent>(OnPurchaseRequested);
            EventBus<ShopPurchaseRequestedEvent>.Register(_purchaseRequestedBinding);

            foreach (IPaymentProcessor processor in _processors.Values)
            {
                if (processor is IRestorablePaymentProcessor restorable)
                {
                    _restorables.Add(restorable);
                    restorable.ProductRestored += OnProductRestored;
                }
            }

            _restoreRequestedBinding = new EventBinding<ShopRestoreRequestedEvent>(OnRestoreRequested);
            EventBus<ShopRestoreRequestedEvent>.Register(_restoreRequestedBinding);
        }

        private void OnPurchaseRequested(ShopPurchaseRequestedEvent evt) => Purchase(evt.ItemId);

        private void OnRestoreRequested(ShopRestoreRequestedEvent evt) => RestorePurchases();

        public PurchaseOutcome Purchase(string itemId)
        {
            if (!_catalog.TryGet(itemId, out IShopItem item))
                return Decline(itemId, CurrencyType.Coin, 0, PurchaseFailureReason.ItemNotFound);

            // 1) Rule chain (max purchases, ownership, custom requirements, …). First block wins.
            for (int i = 0; i < _validators.Count; i++)
            {
                PurchaseFailureReason reason = _validators[i].Validate(item, _context);
                if (reason != PurchaseFailureReason.None)
                    return Decline(item, reason);
            }

            // 2) Capability: we must be able to BOTH charge and deliver before touching anything.
            if (!_processors.TryGetValue(item.Currency, out IPaymentProcessor processor))
            {
                _logger.Error($"No payment processor registered for currency '{item.Currency}' (item '{item.Id}').");
                return Decline(item, PurchaseFailureReason.NoPaymentProcessor);
            }

            for (int i = 0; i < item.Rewards.Count; i++)
            {
                IShopReward reward = item.Rewards[i];
                if (reward == null || !_granters.ContainsKey(reward.GetType()))
                {
                    _logger.Error($"No reward granter for '{(reward == null ? "null" : reward.GetType().Name)}' on item '{item.Id}'.");
                    return Decline(item, PurchaseFailureReason.NoRewardHandler);
                }
            }

            if (!processor.CanPay(item))
                return Decline(item, PurchaseFailureReason.InsufficientFunds);

            // 3) Charge. Async processors (e.g. IAP) report the result later via a callback.
            if (processor is IAsyncPaymentProcessor asyncProcessor)
            {
                _logger.Log($"Awaiting async payment for '{item.Id}'...");
                asyncProcessor.BeginPay(item, paid =>
                {
                    if (paid) Fulfill(item);
                    else Decline(item, PurchaseFailureReason.PaymentFailed);
                });
                return PurchaseOutcome.AsPending(item.Id);
            }

            // Synchronous charge (authoritative). Only deliver if the charge succeeded.
            if (!processor.Pay(item))
                return Decline(item, PurchaseFailureReason.PaymentFailed);

            return Fulfill(item);
        }

        // Grant rewards, record the sale and publish success. Used for sync purchases immediately, and
        // for async (IAP) purchases when the store confirms (callback runs on Unity's main thread).
        private PurchaseOutcome Fulfill(IShopItem item)
        {
            for (int i = 0; i < item.Rewards.Count; i++)
            {
                IShopReward reward = item.Rewards[i];
                _granters[reward.GetType()].Grant(reward);
            }

            _history.Record(new PurchaseRecord(item.Id, item.Currency, item.Price, true,
                PurchaseFailureReason.None, DateTime.UtcNow));
            _logger.Log($"Purchased '{item.Id}' for {item.Price} {item.Currency}.");
            EventBus<ShopPurchaseSucceededEvent>.Raise(new ShopPurchaseSucceededEvent(item));
            return PurchaseOutcome.Ok(item.Id);
        }

        // Re-grant a non-consumable the store reports as already owned (restore or out-of-band purchase).
        private void OnProductRestored(string productId)
        {
            if (_catalog.TryGet(productId, out IShopItem item) && !_history.Owns(productId))
            {
                _logger.Log($"Restoring '{productId}'.");
                Fulfill(item);
            }
        }

        /// <summary>Ask restorable processors (IAP) to restore owned non-consumables; publishes the result.</summary>
        public void RestorePurchases(Action<bool> onComplete = null)
        {
            void Finish(bool success)
            {
                EventBus<ShopPurchasesRestoredEvent>.Raise(new ShopPurchasesRestoredEvent(success));
                onComplete?.Invoke(success);
            }

            if (_restorables.Count == 0)
            {
                _logger.Warn("RestorePurchases: no restorable payment processor registered.");
                Finish(false);
                return;
            }

            int remaining = _restorables.Count;
            bool anySuccess = false;
            foreach (IRestorablePaymentProcessor restorable in _restorables)
            {
                restorable.Restore(success =>
                {
                    anySuccess |= success;
                    if (--remaining == 0) Finish(anySuccess);
                });
            }
        }

        private PurchaseOutcome Decline(IShopItem item, PurchaseFailureReason reason) =>
            Decline(item.Id, item.Currency, item.Price, reason);

        private PurchaseOutcome Decline(string itemId, CurrencyType currency, int price, PurchaseFailureReason reason)
        {
            _history.Record(new PurchaseRecord(itemId, currency, price, false, reason, DateTime.UtcNow));
            _logger.Log($"Purchase declined for '{itemId}': {reason}.");
            EventBus<ShopPurchaseFailedEvent>.Raise(new ShopPurchaseFailedEvent(itemId, reason));
            return PurchaseOutcome.Fail(itemId, reason);
        }

        public void Dispose()
        {
            EventBus<ShopPurchaseRequestedEvent>.Deregister(_purchaseRequestedBinding);
            EventBus<ShopRestoreRequestedEvent>.Deregister(_restoreRequestedBinding);
            foreach (IRestorablePaymentProcessor restorable in _restorables)
                restorable.ProductRestored -= OnProductRestored;
        }

        private sealed class ShopContext : IShopContext
        {
            public IPurchaseHistory History { get; }
            public ShopContext(IPurchaseHistory history) => History = history;
        }
    }
}
