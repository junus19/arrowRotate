using System.Collections.Generic;

namespace GameBrain.Store
{
    /// <summary>
    /// Fluent composition root that wires a <see cref="ShopService"/> with sensible defaults so callers
    /// don't repeat the plumbing. Everything is overridable: swap the wallet, add validators, register
    /// extra payment processors or reward granters. Casual types are fully qualified to avoid clashing
    /// with this system's own <see cref="CurrencyType"/>.
    ///
    /// Example:
    ///   var service = new ShopServiceBuilder(catalog)
    ///       .WithWallet(new CurrencyManagerWallet(currencyManager, gameData))
    ///       .WithBoosters(boosterGameData)
    ///       .AddValidator(new DelegateRequirementValidator(item => playerLevel >= 5))
    ///       .Build();
    /// </summary>
    public sealed class ShopServiceBuilder
    {
        private readonly IShopCatalog _catalog;
        private IWallet _wallet;
        private IPurchaseHistory _history;
        private IShopLogger _logger;
        private GameBrain.Casual.BoosterGameData _boosterData;
        private IIapService _iap;

        private readonly List<IPurchaseValidator> _validators = new List<IPurchaseValidator>();
        private readonly List<IPaymentProcessor> _extraProcessors = new List<IPaymentProcessor>();
        private readonly List<IRewardGranter> _extraGranters = new List<IRewardGranter>();

        public ShopServiceBuilder(IShopCatalog catalog) => _catalog = catalog;

        public ShopServiceBuilder WithWallet(IWallet wallet) { _wallet = wallet; return this; }
        public ShopServiceBuilder WithHistory(IPurchaseHistory history) { _history = history; return this; }
        public ShopServiceBuilder WithLogger(IShopLogger logger) { _logger = logger; return this; }
        public ShopServiceBuilder WithBoosters(GameBrain.Casual.BoosterGameData boosterData) { _boosterData = boosterData; return this; }
        public ShopServiceBuilder WithIap(IIapService iap) { _iap = iap; return this; }
        public ShopServiceBuilder AddValidator(IPurchaseValidator validator) { _validators.Add(validator); return this; }
        public ShopServiceBuilder AddPaymentProcessor(IPaymentProcessor processor) { _extraProcessors.Add(processor); return this; }
        public ShopServiceBuilder AddRewardGranter(IRewardGranter granter) { _extraGranters.Add(granter); return this; }

        public ShopService Build()
        {
            IShopLogger logger = _logger ?? new UnityShopLogger();
            IWallet wallet = _wallet ?? new InMemoryWallet();
            IPurchaseHistory history = _history ?? new InMemoryPurchaseHistory();

            List<IPaymentProcessor> processors = new List<IPaymentProcessor>
            {
                new WalletPaymentProcessor(CurrencyType.Coin, wallet),
                new WalletPaymentProcessor(CurrencyType.Gem, wallet)
            };
            if (_iap != null) processors.Add(new RealMoneyPaymentProcessor(_iap, logger));
            processors.AddRange(_extraProcessors);

            List<IRewardGranter> granters = new List<IRewardGranter>
            {
                new CurrencyRewardGranter(wallet)
            };
            if (_boosterData != null) granters.Add(new BoosterRewardGranter(_boosterData));
            granters.AddRange(_extraGranters);

            List<IPurchaseValidator> validators = new List<IPurchaseValidator>
            {
                new OwnershipValidator(),
                new MaxPurchaseValidator()
            };
            validators.AddRange(_validators);

            return new ShopService(_catalog, processors, granters, validators, history, logger);
        }
    }
}
