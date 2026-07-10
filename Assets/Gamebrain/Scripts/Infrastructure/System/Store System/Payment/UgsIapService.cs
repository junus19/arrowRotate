using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing;

namespace GameBrain.Store
{
    /// <summary>A RealMoney product to register with the store (id must match the UGS dashboard product).</summary>
    public readonly struct UgsProduct
    {
        public readonly string Id;
        public readonly bool Consumable;
        public UgsProduct(string id, bool consumable) { Id = id; Consumable = consumable; }
    }

    /// <summary>
    /// Unity IAP 5.x (Unity Gaming Services) implementation of <see cref="IIapService"/>. Connects the
    /// StoreController, fetches products, and routes <see cref="Purchase"/> through PurchaseProduct; results
    /// arrive via StoreController order events (Unity main thread). Owned/restored products surface as
    /// pending orders with no in-flight purchase and are reported through <see cref="PurchaseRestored"/>.
    /// </summary>
    public sealed class UgsIapService : IIapService
    {
        private readonly List<UgsProduct> _products;
        private readonly IShopLogger _logger;
        private readonly Dictionary<string, Action<bool>> _pending = new Dictionary<string, Action<bool>>();
        private StoreController _controller;
        private bool _ready;

        public bool IsReady => _ready;

        public event Action<string> PurchaseRestored;

        public UgsIapService(IEnumerable<UgsProduct> products, IShopLogger logger = null)
        {
            _products = new List<UgsProduct>(products);
            _logger = logger ?? new UnityShopLogger();
            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            _controller = UnityIAPServices.StoreController();

            _controller.OnStoreConnected += OnStoreConnected;
            _controller.OnStoreDisconnected += OnStoreDisconnected;
            _controller.OnProductsFetched += OnProductsFetched;
            _controller.OnProductsFetchFailed += OnProductsFetchFailed;
            _controller.OnPurchasePending += OnPurchasePending;
            _controller.OnPurchaseFailed += OnPurchaseFailed;

            try
            {
                await _controller.Connect();
            }
            catch (Exception e)
            {
                _logger.Error($"IAP connect failed: {e.Message}");
            }
        }

        private void OnStoreConnected()
        {
            List<ProductDefinition> definitions = new List<ProductDefinition>(_products.Count);
            foreach (UgsProduct p in _products)
                definitions.Add(new ProductDefinition(p.Id, p.Consumable ? ProductType.Consumable : ProductType.NonConsumable));

            _controller.FetchProducts(definitions);
        }

        private void OnStoreDisconnected(StoreConnectionFailureDescription description)
        {
            _ready = false;
            _logger.Warn($"IAP store disconnected: {description.message}");
        }

        private void OnProductsFetched(List<Product> products)
        {
            _ready = true;
            _logger.Log($"IAP products fetched: {products.Count}.");
        }

        private void OnProductsFetchFailed(ProductFetchFailed failure) =>
            _logger.Error($"IAP products fetch failed: {failure.FailureReason}");

        public bool IsAvailable(string productId)
        {
            if (_controller == null) return false;
            Product product = _controller.GetProducts().FirstOrDefault(p => p.definition.id == productId);
            return product != null && product.availableToPurchase;
        }

        public void Purchase(string productId, Action<bool> onComplete)
        {
            if (!IsAvailable(productId))
            {
                _logger.Warn($"Product '{productId}' is not available.");
                onComplete?.Invoke(false);
                return;
            }

            _pending[productId] = onComplete;
            _controller.PurchaseProduct(productId);
        }

        public void RestorePurchases(Action<bool> onComplete)
        {
            if (_controller == null) { onComplete?.Invoke(false); return; }
            _controller.RestoreTransactions((success, error) =>
            {
                if (!success) _logger.Warn($"Restore failed: {error}");
                onComplete?.Invoke(success);
            });
        }

        private void OnPurchasePending(PendingOrder order)
        {
            string id = FirstProductId(order);
            if (id != null)
            {
                if (_pending.TryGetValue(id, out Action<bool> callback))
                {
                    _pending.Remove(id);
                    callback?.Invoke(true);             // resolves an in-flight purchase
                }
                else
                {
                    PurchaseRestored?.Invoke(id);        // restore or out-of-band (owned) product
                }
            }

            _controller.ConfirmPurchase(order);          // finalize the order
        }

        private void OnPurchaseFailed(FailedOrder order)
        {
            string id = FirstProductId(order);
            _logger.Warn($"Purchase failed: {id} ({order.FailureReason})");
            if (id != null && _pending.TryGetValue(id, out Action<bool> callback))
            {
                _pending.Remove(id);
                callback?.Invoke(false);
            }
        }

        private static string FirstProductId(Order order) =>
            order?.CartOrdered?.Items()?.FirstOrDefault()?.Product?.definition?.id;
    }
}
