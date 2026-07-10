using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class CurrencyUpdatedEvent : IEvent
    {
        private readonly int _coinAmount;
        public int CoinAmount => _coinAmount;

        public CurrencyUpdatedEvent(int coinAmount)
        {
            _coinAmount = coinAmount;
        }
    }
}
