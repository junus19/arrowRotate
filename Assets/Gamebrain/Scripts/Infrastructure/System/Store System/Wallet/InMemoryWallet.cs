using System.Collections.Generic;

namespace GameBrain.Store
{
    /// <summary>
    /// Self-contained wallet for tests and the demo. Holds balances in memory and is not tied to game
    /// save data. For production use <see cref="CurrencyManagerWallet"/>.
    /// </summary>
    public sealed class InMemoryWallet : IWallet
    {
        private readonly Dictionary<CurrencyType, int> _balances = new Dictionary<CurrencyType, int>();

        public InMemoryWallet(int coin = 0, int gem = 0)
        {
            _balances[CurrencyType.Coin] = coin;
            _balances[CurrencyType.Gem] = gem;
        }

        public int GetBalance(CurrencyType currency) =>
            _balances.TryGetValue(currency, out int value) ? value : 0;

        public bool CanAfford(CurrencyType currency, int amount) =>
            amount >= 0 && GetBalance(currency) >= amount;

        public bool TrySpend(CurrencyType currency, int amount)
        {
            if (!CanAfford(currency, amount)) return false;
            _balances[currency] = GetBalance(currency) - amount;
            return true;
        }

        public void Deposit(CurrencyType currency, int amount)
        {
            if (amount <= 0) return;
            _balances[currency] = GetBalance(currency) + amount;
        }
    }
}
