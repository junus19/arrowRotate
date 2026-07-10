using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class CurrencyManager
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

        public void AddCoin(int amount)
        {
            _gameData.AddCoin(amount);
            OnCurrencyUpdated();
        }

        public void DebitCoin(int amount)
        {
            if (CanSpendCoin(amount))
            {
                _gameData.DebitCoin(amount);
                OnCurrencyUpdated();
            }
        }

        public bool CanSpendCoin(int amount)
        {
            return _gameData.Data.Coin >= amount;
        }

        private void OnCurrencyUpdated()
        {
            int coin = _gameData.GetCoinAmount();
            EventBus<CurrencyUpdatedEvent>.Raise(new CurrencyUpdatedEvent(coin));
        }
    }
}
