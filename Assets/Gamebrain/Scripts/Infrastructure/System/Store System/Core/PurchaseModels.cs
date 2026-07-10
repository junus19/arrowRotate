using System;

namespace GameBrain.Store
{
    /// <summary>Immutable result of a single purchase attempt.</summary>
    public readonly struct PurchaseOutcome
    {
        public bool Success { get; }
        /// <summary>True when an async payment (e.g. IAP) was started; the final result arrives via events.</summary>
        public bool Pending { get; }
        public string ItemId { get; }
        public PurchaseFailureReason Reason { get; }

        private PurchaseOutcome(bool success, bool pending, string itemId, PurchaseFailureReason reason)
        {
            Success = success;
            Pending = pending;
            ItemId = itemId;
            Reason = reason;
        }

        public static PurchaseOutcome Ok(string itemId) =>
            new PurchaseOutcome(true, false, itemId, PurchaseFailureReason.None);

        public static PurchaseOutcome Fail(string itemId, PurchaseFailureReason reason) =>
            new PurchaseOutcome(false, false, itemId, reason);

        /// <summary>The purchase was started asynchronously (IAP); watch for the succeeded/failed event.</summary>
        public static PurchaseOutcome AsPending(string itemId) =>
            new PurchaseOutcome(false, true, itemId, PurchaseFailureReason.None);

        public override string ToString() =>
            Success ? $"Purchase OK: {ItemId}"
            : Pending ? $"Purchase PENDING: {ItemId}"
            : $"Purchase FAILED: {ItemId} ({Reason})";
    }

    /// <summary>A recorded purchase attempt (successful or not). Used for history, caps and ownership.</summary>
    public sealed class PurchaseRecord
    {
        public string ItemId { get; }
        public CurrencyType Currency { get; }
        public int Price { get; }
        public bool Success { get; }
        public PurchaseFailureReason Reason { get; }
        public DateTime TimeUtc { get; }

        public PurchaseRecord(string itemId, CurrencyType currency, int price, bool success,
            PurchaseFailureReason reason, DateTime timeUtc)
        {
            ItemId = itemId;
            Currency = currency;
            Price = price;
            Success = success;
            Reason = reason;
            TimeUtc = timeUtc;
        }
    }
}
