using GameBrain.Store;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class CurrencyManager : IWallet
    {
        private readonly GameData _gameData;

        public CurrencyManager(GameData gameData)
        {
            _gameData = gameData;
        }

        public void Init()
        {
            OnCurrencyUpdated();
        }

        private void OnCurrencyUpdated()
        {
            int coin = _gameData.GetCoinAmount();
            EventBus<CurrencyUpdatedEvent>.Raise(new CurrencyUpdatedEvent(coin));
        }

        public int GetBalance(CurrencyType currency)
        {
            return currency == CurrencyType.Coin ? _gameData.GetCoinAmount() : 0;
        }

        public bool CanAfford(CurrencyType currency, int amount)
        {
            if (currency == CurrencyType.Coin)
                return _gameData.Data.Coin >= amount;
            return false;
        }

        public bool TrySpend(CurrencyType currencyType, int amount)
        {
            if (!CanAfford(currencyType, amount)) return false;
            if (currencyType == CurrencyType.Coin)
            {
                _gameData.DebitCoin(amount);
                OnCurrencyUpdated();
                return true;
            }
            return false;
        }

        public void Deposit(CurrencyType currencyType, int amount)
        {
            if (currencyType == CurrencyType.Coin)
            {
                _gameData.AddCoin(amount);
                OnCurrencyUpdated();
            }
        }
    }
}
