using System;

namespace GameBrain.Store
{
    /// <summary>Blocks a purchase once the item's per-player purchase cap is reached.</summary>
    public sealed class MaxPurchaseValidator : IPurchaseValidator
    {
        public PurchaseFailureReason Validate(IShopItem item, IShopContext context)
        {
            if (item.MaxPurchases < 0) return PurchaseFailureReason.None; // unlimited
            return context.History.GetPurchaseCount(item.Id) >= item.MaxPurchases
                ? PurchaseFailureReason.MaxPurchasesReached
                : PurchaseFailureReason.None;
        }
    }

    /// <summary>Blocks re-buying a non-consumable the player already owns.</summary>
    public sealed class OwnershipValidator : IPurchaseValidator
    {
        public PurchaseFailureReason Validate(IShopItem item, IShopContext context)
        {
            if (item.IsConsumable) return PurchaseFailureReason.None;
            return context.History.Owns(item.Id)
                ? PurchaseFailureReason.AlreadyOwned
                : PurchaseFailureReason.None;
        }
    }

    /// <summary>
    /// Generic, closure-based rule. Use it to add requirements (level locks, time gates, A/B flags)
    /// without writing a dedicated validator class each time.
    /// </summary>
    public sealed class DelegateRequirementValidator : IPurchaseValidator
    {
        private readonly Func<IShopItem, bool> _isSatisfied;
        private readonly PurchaseFailureReason _reasonIfUnsatisfied;

        public DelegateRequirementValidator(Func<IShopItem, bool> isSatisfied,
            PurchaseFailureReason reasonIfUnsatisfied = PurchaseFailureReason.RequirementNotMet)
        {
            _isSatisfied = isSatisfied;
            _reasonIfUnsatisfied = reasonIfUnsatisfied;
        }

        public PurchaseFailureReason Validate(IShopItem item, IShopContext context)
        {
            if (_isSatisfied == null) return PurchaseFailureReason.None;
            return _isSatisfied(item) ? PurchaseFailureReason.None : _reasonIfUnsatisfied;
        }
    }
}
