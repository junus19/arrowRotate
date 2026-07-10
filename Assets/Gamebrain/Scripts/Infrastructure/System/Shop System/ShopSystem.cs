using System;
using UnityEngine;
using System.Linq;
using GameBrain.Utils;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class ShopSystem
    {
        private readonly List<ShopItemData> _shopItems;
        private readonly List<ShopCategoryData> _categories;
        private readonly List<PurchaseTransaction> _transactionHistory;
        private readonly EventBinding<ShopItemPurchaseRequestEvent> _shopItemPurchaseRequestEventBinding;
        private readonly CurrencyManager _currencyManager;
        private readonly GameData _gameData;
        private readonly BoosterGameData _boosterGameData;
        
        public List<PurchaseTransaction> TransactionHistory => _transactionHistory;
        public event Action<string, PurchaseResult> OnPurchaseAttempt;
        public event Action<string> OnPurchaseSuccess;
        public event Action<string> OnPurchaseFailed;

        public ShopSystem(ShopCatalogData catalogData, CurrencyManager currencyManager)
        {
            _currencyManager = currencyManager;
            _categories = new List<ShopCategoryData>();
            _shopItems = new List<ShopItemData>();
            _transactionHistory = new List<PurchaseTransaction>();
            _categories.Clear();
            _shopItems.Clear();

            foreach (ShopCategoryData categoryData in catalogData.Categories)
            {
                _categories.Add(categoryData);

                foreach (ShopItemData item in categoryData.Items)
                {
                    _shopItems.Add(item);
                }
            }

            _shopItemPurchaseRequestEventBinding = new EventBinding<ShopItemPurchaseRequestEvent>(OnShopItemPurchaseRequested);
            EventBus<ShopItemPurchaseRequestEvent>.Register(_shopItemPurchaseRequestEventBinding);
        }

        private void OnShopItemPurchaseRequested(ShopItemPurchaseRequestEvent eventInfo)
        {
            BuyItem(eventInfo.Data.Id);
        }

        private PurchaseResult PurchaseItem(string itemId)
        {
            Debug.Log($"Trying to purchase: {itemId}");
            
            ShopItemData shopItemData = _shopItems.FirstOrDefault(item => item.Id  == itemId);
            if (shopItemData == null)
                return LogTransaction(itemId, 0, CurrencyType.Coin, PurchaseResult.ItemNotAvailable);

            // Not enough soft currency!
            Debug.Log($"Item Price: {(int)shopItemData.Price}");
            if (shopItemData.CurrencyType == CurrencyType.Coin && !_currencyManager.CanSpendCoin((int)shopItemData.Price))
                return LogTransaction(itemId, shopItemData.Price, CurrencyType.Coin, PurchaseResult.InsufficientFunds);

            // if (!shopItemData.CanPurchase())
            //     return LogTransaction(itemId, shopItemData.Price, shopItemData.CurrencyType, PurchaseResult.MaxPurchasesReached);

            // if (!purchaseValidator.ValidatePurchase(item, playerLevel))
            //     return LogTransaction(itemId, shopItemData.Price, shopItemData.CurrencyType, PurchaseResult.RequirementNotMet);
            //
            // if (!currencyManager.SpendCurrency(shopItemData.CurrencyType, shopItemData.Price))
            //     return LogTransaction(itemId, shopItemData.Price, shopItemData.CurrencyType, PurchaseResult.InsufficientFunds);
            
            // inventoryManager.AddItem(itemId);

            OnPurchaseSuccess?.Invoke(shopItemData.Id);
            return LogTransaction(itemId, shopItemData.Price, shopItemData.CurrencyType, PurchaseResult.Success);
        }

        private PurchaseResult LogTransaction(string itemId, float price, CurrencyType currency, PurchaseResult result)
        {
            PurchaseTransaction transaction = new PurchaseTransaction(itemId, price, currency, result);
            _transactionHistory.Add(transaction);

            ShopItemData shopItemData = _shopItems.FirstOrDefault(item => item.Id  == itemId);
            
            if (shopItemData != null)
            {
                OnPurchaseAttempt?.Invoke(shopItemData.Id, result);
                if (result != PurchaseResult.Success)
                    OnPurchaseFailed?.Invoke(GetFailureMessage(result));
            }

            return result;
        }

        private static string GetFailureMessage(PurchaseResult result)
        {
            return result switch
            {
                PurchaseResult.InsufficientFunds => "Not enough currency!",
                PurchaseResult.ItemNotAvailable => "Item not available!",
                PurchaseResult.MaxPurchasesReached => "Maximum purchases reached!",
                PurchaseResult.RequirementNotMet => "Requirements not met!",
                _ => "Purchase failed!"
            };
        }

        public ShopItemData GetItem(string itemId)
        {
            ShopItemData shopItemData = _shopItems.FirstOrDefault(item => item.Id  == itemId);
            return shopItemData;
        }
        
        public bool TryGetItem(string itemId, out ShopItemData itemData)
        {
            itemData = _shopItems.FirstOrDefault(item => item.Id == itemId);
            return itemData != null;
        }

        public void BuyItem(string itemId)
        {
            PurchaseResult result = PurchaseItem(itemId);
            Debug.Log(result);
            Debug.Log(_transactionHistory.Last());

            if (result == PurchaseResult.Success)
                HandlePurchaseSuccess(itemId);
            else
                HandlePurchaseFailed(itemId, GetFailureMessage(result));
        }

        private void HandlePurchaseSuccess(string itemId)
        {
            ShopItemData shopItemData = GetItem(itemId);
            if (shopItemData.CurrencyType == CurrencyType.Coin)
            {
                _currencyManager.DebitCoin((int)shopItemData.Price);
                foreach (ShopItemContent content in shopItemData.Contents)
                {
                    if (content.ItemType == ShopItemType.Coin)
                    {
                        _currencyManager.AddCoin(content.Amount);
                    }

                    if (content.ItemType == ShopItemType.Booster)
                    {
                        _boosterGameData.AddBooster(content.BoosterType, content.Amount);
                    }
                }
            }
            else if (shopItemData.CurrencyType == CurrencyType.RealMoney)
            {
                // TODO: Implement IAP!
            }
            Debug.Log($"Successfully purchased: {itemId}");
        }

        private void HandlePurchaseFailed(string itemId, string message)
        {
            Debug.Log($"Purchase failed: {message}");
            EventBus<ShopItemTransactionDeclinedEvent>.Raise(new ShopItemTransactionDeclinedEvent(GetItem(itemId), message));
            OnPurchaseFailed?.Invoke(message);
        }
    }
}
