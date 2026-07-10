using System.Collections.Generic;

namespace GameBrain.Store
{
    /// <summary>Records purchase attempts and answers ownership / purchase-count questions.</summary>
    public interface IPurchaseHistory
    {
        void Record(PurchaseRecord record);

        /// <summary>Number of <b>successful</b> purchases of the given item.</summary>
        int GetPurchaseCount(string itemId);

        /// <summary>True if the item has been successfully purchased at least once.</summary>
        bool Owns(string itemId);

        IReadOnlyList<PurchaseRecord> Records { get; }
    }
}
