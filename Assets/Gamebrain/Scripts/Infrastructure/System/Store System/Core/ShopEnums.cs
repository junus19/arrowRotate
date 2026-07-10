namespace GameBrain.Store
{
    /// <summary>Currency an item is priced in. Add a value here only when a new payment kind exists.</summary>
    public enum CurrencyType
    {
        Coin,
        Gem,
        RealMoney
    }

    /// <summary>
    /// Outcome reason of a purchase attempt. <see cref="None"/> means success; every other value is a
    /// machine-readable failure cause. UI maps these to localized text — the core never produces strings.
    /// </summary>
    public enum PurchaseFailureReason
    {
        None = 0,
        ItemNotFound,
        InsufficientFunds,
        MaxPurchasesReached,
        AlreadyOwned,
        RequirementNotMet,
        NoPaymentProcessor,
        NoRewardHandler,
        PaymentFailed
    }
}
