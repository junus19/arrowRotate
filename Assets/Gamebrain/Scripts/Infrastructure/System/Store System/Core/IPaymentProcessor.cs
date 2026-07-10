namespace GameBrain.Store
{
    /// <summary>
    /// Pays for an item in one currency. Register one processor per <see cref="CurrencyType"/>.
    /// Adding a new currency means adding a processor, not editing the shop core (Open/Closed).
    /// </summary>
    public interface IPaymentProcessor
    {
        CurrencyType Currency { get; }

        /// <summary>Can this item be paid for right now (sufficient funds / product available)?</summary>
        bool CanPay(IShopItem item);

        /// <summary>Charge for the item. Returns false if the charge did not go through.</summary>
        bool Pay(IShopItem item);
    }
}
