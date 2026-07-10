using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class SettingsPanel : UIPanel
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private SwitchUIItemController _hapticSwitch;
        [SerializeField] private SwitchUIItemController _soundSwitch;

        private EventBinding<OpenSettingsPanelEvent> _openSettingsPanel;

        public void InitPanel(bool isHapticOn, bool isSoundOn)
        {
            _hapticSwitch.Init(isHapticOn);
            _soundSwitch.Init(isSoundOn);

            _closeButton.onClick.AddListener(OnCloseSettingsPanel);
            _openSettingsPanel = new EventBinding<OpenSettingsPanelEvent>(OnOpenSettingsPanel);
            EventBus<OpenSettingsPanelEvent>.Register(_openSettingsPanel);
        }

        private void OnOpenSettingsPanel()
        {
            gameObject.SetActive(true);
        }

        private void OnCloseSettingsPanel()
        {
            gameObject.SetActive(false);
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
        }

        private void OnDestroy()
        {
            EventBus<OpenSettingsPanelEvent>.Deregister(_openSettingsPanel);
        }
    }
}
