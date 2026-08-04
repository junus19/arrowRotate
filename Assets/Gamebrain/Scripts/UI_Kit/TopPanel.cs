using System;
using GameBrain.Store;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class TopPanel : UIPanel
    {
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _addCoinButton;
        [SerializeField] private TextMeshProUGUI coinText;
        private CurrencyManager _currencyManager;

        protected EventBinding<OnStoreOpenedEvent> _onStoreOpenedEvent;
        protected EventBinding<OnStoreClosedEvent> _onStoreClosedEvent;
        protected EventBinding<CurrencyUpdatedEvent> _onCurrencyUpdatedEvent;

        protected override void Awake()
        {
            base.Awake();
            _onStoreOpenedEvent = new EventBinding<OnStoreOpenedEvent>(OnStoreOpened);
            _onStoreClosedEvent = new EventBinding<OnStoreClosedEvent>(OnStoreClosed);
            _onCurrencyUpdatedEvent = new EventBinding<CurrencyUpdatedEvent>(OnCurrencyUpdated);

            _addCoinButton.onClick.AddListener(OnAddCoinButton);
        }
        
        private void OnEnable()
        {
            _settingsButton.onClick.AddListener(OnSettingsButton);
            EventBus<OnStoreOpenedEvent>.Register(_onStoreOpenedEvent);
            EventBus<OnStoreClosedEvent>.Register(_onStoreClosedEvent);
            EventBus<CurrencyUpdatedEvent>.Register(_onCurrencyUpdatedEvent);
            if (_currencyManager != null)
                coinText.text = _currencyManager.GetBalance(CurrencyType.Coin).ToString();
        }

        private void OnDisable()
        {
            _settingsButton.onClick.RemoveListener(OnSettingsButton);
            EventBus<OnStoreOpenedEvent>.Deregister(_onStoreOpenedEvent);
            EventBus<OnStoreClosedEvent>.Deregister(_onStoreClosedEvent);
            EventBus<CurrencyUpdatedEvent>.Deregister(_onCurrencyUpdatedEvent);
        }

        public override void OnInject(object[] args)
        {
            base.OnInject(args);
            _currencyManager = (CurrencyManager)args[0];
            coinText.text = _currencyManager.GetBalance(CurrencyType.Coin).ToString();
        }

        private void OnSettingsButton()
        {
            EventBus<OpenSettingsPanelEvent>.Raise(new OpenSettingsPanelEvent());
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
        }

        private void OnAddCoinButton()
        {
            Debug.Log("Open Store");

            FindFirstObjectByType<NavBar>().TrySelect(0);
        }

        public void SetStatusOfTopBarButtons(bool isAddCoinButtonAvailable, bool isSettingButtonAvailable)
        {
            _addCoinButton.gameObject.SetActive(isAddCoinButtonAvailable);
            _settingsButton.gameObject.SetActive(isSettingButtonAvailable);
        }

        private void OnStoreOpened(OnStoreOpenedEvent eventInfo)
        {
            SetStatusOfTopBarButtons(false, false);
        }

        private void OnStoreClosed(OnStoreClosedEvent eventInfo)
        {
            SetStatusOfTopBarButtons(true, true);
        }

        private void OnCurrencyUpdated(CurrencyUpdatedEvent eventInfo)
        {
            coinText.text = eventInfo.CoinAmount.ToString();
        }
    }
}
