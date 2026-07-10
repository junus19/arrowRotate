using UnityEngine;
using GameBrain.Utils;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class ShopPanel : UIPanel
    {
        [SerializeField] protected ShopCatalogData _shopCatalogData;
        [SerializeField] protected Transform _contentContainer;
        [SerializeField] protected PopUp_TransactionDeclined _transactionDeclinedPopup;
        protected List<ShopItemView> _shopItems;
        protected EventBinding<ShopItemTransactionDeclinedEvent> shopItemTransactionDeclinedEventBinding;

        protected override void Awake()
        {
            base.Awake();
            Initialize();
            shopItemTransactionDeclinedEventBinding = new EventBinding<ShopItemTransactionDeclinedEvent>(OnPurchaseFailed);
        }

        protected virtual void Initialize()
        {
            foreach (ShopCategoryData categoryData in _shopCatalogData.Categories)
            {
                ShopCategoryView categoryView = Instantiate(categoryData.ViewPrefab, _contentContainer);
                categoryView.Init(categoryData);
            }
        }
        
        protected virtual void OnEnable()
        {
            EventBus<ShopItemTransactionDeclinedEvent>.Register(shopItemTransactionDeclinedEventBinding);
            _transactionDeclinedPopup.gameObject.SetActive(false);
        }

        protected virtual  void OnDisable()
        {
            EventBus<ShopItemTransactionDeclinedEvent>.Deregister(shopItemTransactionDeclinedEventBinding);
        }

        protected virtual void OnPurchaseFailed(ShopItemTransactionDeclinedEvent eventInfo)
        {
            _transactionDeclinedPopup.SetTitle("Purchase Failed");
            _transactionDeclinedPopup.SetResultText(eventInfo.Message);
            _transactionDeclinedPopup.Open();
        }
    }
}
