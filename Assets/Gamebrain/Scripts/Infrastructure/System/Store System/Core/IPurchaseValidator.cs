namespace GameBrain.Store
{
    /// <summary>Read-only state a validator may consult while deciding whether a purchase is allowed.</summary>
    public interface IShopContext
    {
        IPurchaseHistory History { get; }
    }

    /// <summary>
    /// One purchase rule. Return <see cref="PurchaseFailureReason.None"/> to allow, or a reason to block.
    /// Validators run as an ordered chain; the first blocking reason wins. Add a rule by adding a
    /// validator — the core stays untouched.
    /// </summary>
    public interface IPurchaseValidator
    {
        PurchaseFailureReason Validate(IShopItem item, IShopContext context);
    }
}
