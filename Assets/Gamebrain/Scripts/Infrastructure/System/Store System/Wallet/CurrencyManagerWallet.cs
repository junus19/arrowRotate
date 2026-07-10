namespace GameBrain.Store
{
    /// <summary>
    /// Bridges <see cref="IWallet"/> to the game's currency authority. All Coin mutations go through
    /// <c>CurrencyManager</c> (the only code allowed to change currency); balance reads go through
    /// <c>GameData</c>. Gem is not backed by save data yet — extend the Gem branches here together with
    /// GameData/CurrencyManager when it is. Casual types are fully qualified to avoid clashing with this
    /// system's own <see cref="CurrencyType"/>.
    /// </summary>
    public sealed class CurrencyManagerWallet : IWallet
    {
        private readonly GameBrain.Casual.CurrencyManager _currencyManager;
        private readonly GameBrain.Casual.GameData _gameData;
        private readonly IShopLogger _logger;

        public CurrencyManagerWallet(GameBrain.Casual.CurrencyManager currencyManager,
            GameBrain.Casual.GameData gameData, IShopLogger logger = null)
        {
            _currencyManager = currencyManager;
            _gameData = gameData;
            _logger = logger ?? NullShopLogger.Instance;
        }

        public int GetBalance(CurrencyType currency)
        {
            if (currency == CurrencyType.Coin) return _gameData.GetCoinAmount();
            WarnUnsupported(currency);
            return 0;
        }

        public bool CanAfford(CurrencyType currency, int amount)
        {
            if (currency == CurrencyType.Coin) return _currencyManager.CanSpendCoin(amount);
            WarnUnsupported(currency);
            return false;
        }

        public bool TrySpend(CurrencyType currency, int amount)
        {
            if (currency != CurrencyType.Coin)
            {
                WarnUnsupported(currency);
                return false;
            }

            if (!_currencyManager.CanSpendCoin(amount)) return false;
            _currencyManager.DebitCoin(amount);
            return true;
        }

        public void Deposit(CurrencyType currency, int amount)
        {
            if (amount <= 0) return;

            if (currency == CurrencyType.Coin)
            {
                _currencyManager.AddCoin(amount);
                return;
            }

            WarnUnsupported(currency);
        }

        private void WarnUnsupported(CurrencyType currency) =>
            _logger.Warn($"CurrencyManagerWallet does not back '{currency}' yet — extend GameData/CurrencyManager first.");
    }
}
