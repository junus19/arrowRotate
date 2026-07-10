using System;

namespace GameBrain.Casual
{
    [Serializable]
    public class PurchaseTransaction
    {
        private string _itemId;
        private float _price;
        private CurrencyType _currency;
        private DateTime _purchaseTime;
        private PurchaseResult _result;

        public string ItemId => _itemId;
        public float Price => _price;
        public CurrencyType Currency => _currency;
        public DateTime PurchaseTime => _purchaseTime;
        public PurchaseResult Result => _result;

        public PurchaseTransaction(string itemId, float price, CurrencyType currency, PurchaseResult result)
        {
            _itemId = itemId;
            _price = price;
            _currency = currency;
            _purchaseTime = DateTime.Now;
            _result = result;
        }
    }
}
