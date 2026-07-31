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
        private void OnEnable()
        {
            _settingsButton.onClick.AddListener(OnSettingsButton);
        }

        private void OnDisable()
        {
            _settingsButton.onClick.RemoveListener(OnSettingsButton);
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

    }
}
