using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;
using TMPro;

namespace GameBrain.Casual
{
    public class TopPanel : UIPanel
    {
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _addCoinButton;
        [SerializeField] private TextMeshProUGUI coinText;

        protected EventBinding<OnStoreOpenedEvent> _onStoreOpenedEvent;
        protected EventBinding<OnStoreClosedEvent> _onStoreClosedEvent;

        protected override void Awake()
        {
            base.Awake();
                _onStoreOpenedEvent = new EventBinding<OnStoreOpenedEvent>(OnStoreOpened);
            _onStoreClosedEvent = new EventBinding<OnStoreClosedEvent>(OnStoreClosed);
        }

        private void OnEnable()
        {
            _settingsButton.onClick.AddListener(OnSettingsButton);
        

            EventBus<OnStoreOpenedEvent>.Register(_onStoreOpenedEvent);
            EventBus<OnStoreClosedEvent>.Register(_onStoreClosedEvent);
        }

        private void OnDisable()
        {
            _settingsButton.onClick.RemoveListener(OnSettingsButton);
                        EventBus<OnStoreOpenedEvent>.Deregister(_onStoreOpenedEvent);
            EventBus<OnStoreClosedEvent>.Deregister(_onStoreClosedEvent);

        }

        private void OnSettingsButton()
        {
            EventBus<OpenSettingsPanelEvent>.Raise(new OpenSettingsPanelEvent());
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
        }

        private void OnAddCoinButton()
        {
            Debug.Log("Open Store");
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

    }
}
