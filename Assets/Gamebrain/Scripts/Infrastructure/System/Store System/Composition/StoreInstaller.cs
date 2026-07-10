using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.Store
{
    /// <summary>
    /// Creates a live <see cref="ShopService"/> for a scene so the Store UI actually functions: it builds
    /// the catalog, wires the wallet/payment/rewards via <see cref="ShopServiceBuilder"/>, and stays
    /// subscribed to the EventBus until destroyed.
    ///
    /// The demo default uses an in-memory wallet (touches no save data). For production, swap to a
    /// CurrencyManagerWallet built from the game's CurrencyManager/GameData — see the commented block.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StoreInstaller : MonoBehaviour
    {
        [SerializeField] private ShopCatalogDefinition _catalog;

        [Header("Demo wallet (in-memory)")]
        [SerializeField] private int _startCoins = 50;
        [SerializeField] private int _startGems = 0;

        [Header("IAP")]
        [Tooltip("On: real Unity IAP (UGS). Off: in-editor stub that always succeeds.")]
        [SerializeField] private bool _useRealIap = false;

        private ShopService _service;

        public IShopService Service => _service;

        private void Awake()
        {
            if (_catalog == null)
            {
                Debug.LogError("[Store] StoreInstaller has no catalog assigned.");
                return;
            }

            IShopCatalog catalog = _catalog.BuildCatalog(new UnityShopLogger());

            UnityShopLogger logger = new UnityShopLogger();
            IIapService iap = _useRealIap ? CreateUgsIap(logger) : (IIapService)new StubIapService();

            _service = new ShopServiceBuilder(catalog)
                .WithWallet(new InMemoryWallet(_startCoins, _startGems))
                .WithLogger(logger)
                .WithIap(iap)
                .Build();

            // --- Production: swap the in-memory wallet for the game's currency authority ---
            // _service = new ShopServiceBuilder(catalog)
            //     .WithWallet(new CurrencyManagerWallet(currencyManager, gameData))
            //     .WithBoosters(boosterGameData)
            //     .WithIap(iap)
            //     .Build();
        }

        // Builds a UGS IAP service from the catalog's RealMoney products (id + consumable flag).
        private IIapService CreateUgsIap(IShopLogger logger)
        {
            List<UgsProduct> products = new List<UgsProduct>();
            foreach (ShopItemDefinition definition in _catalog.Items)
            {
                if (definition != null && definition.Currency == CurrencyType.RealMoney)
                    products.Add(new UgsProduct(definition.Id, definition.IsConsumable));
            }
            return new UgsIapService(products, logger);
        }

        private void OnDestroy()
        {
            if (_service != null) _service.Dispose();
        }
    }
}
