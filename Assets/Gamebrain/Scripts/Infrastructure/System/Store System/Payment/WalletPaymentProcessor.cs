namespace GameBrain.Store
{
    /// <summary>Pays for an item using virtual currency held in an <see cref="IWallet"/>.</summary>
    public sealed class WalletPaymentProcessor : IPaymentProcessor
    {
        private readonly IWallet _wallet;

        public CurrencyType Currency { get; }

        public WalletPaymentProcessor(CurrencyType currency, IWallet wallet)
        {
            Currency = currency;
            _wallet = wallet;
        }

        public bool CanPay(IShopItem item) => _wallet.CanAfford(Currency, item.Price);

        public bool Pay(IShopItem item) => _wallet.TrySpend(Currency, item.Price);
    }
}
