namespace GameBrain.Store
{
    /// <summary>
    /// Abstraction over virtual-currency balances. Implementations decide where the balance lives:
    /// in memory for tests, or routed through the game's currency authority for production.
    /// </summary>
    public interface IWallet
    {
        int GetBalance(CurrencyType currency);
        bool CanAfford(CurrencyType currency, int amount);
        bool TrySpend(CurrencyType currency, int amount);
        void Deposit(CurrencyType currency, int amount);
    }
}
