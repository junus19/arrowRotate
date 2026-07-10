using System.Collections.Generic;

namespace GameBrain.Store
{
    /// <summary>
    /// In-memory purchase history. Swap for a save-backed implementation (e.g. routed through
    /// DataManager) when persistence is needed — the shop only depends on <see cref="IPurchaseHistory"/>.
    /// </summary>
    public sealed class InMemoryPurchaseHistory : IPurchaseHistory
    {
        private readonly List<PurchaseRecord> _records = new List<PurchaseRecord>();

        public IReadOnlyList<PurchaseRecord> Records => _records;

        public void Record(PurchaseRecord record)
        {
            if (record != null) _records.Add(record);
        }

        public int GetPurchaseCount(string itemId)
        {
            int count = 0;
            for (int i = 0; i < _records.Count; i++)
            {
                PurchaseRecord record = _records[i];
                if (record.Success && record.ItemId == itemId) count++;
            }
            return count;
        }

        public bool Owns(string itemId) => GetPurchaseCount(itemId) > 0;
    }
}
