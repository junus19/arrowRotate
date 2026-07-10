using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class HomePanel : UIPanel
    {
        [SerializeField] private RectTransform buttonContainer;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private TMP_Text _levelText;

        private void OnEnable()
        {
            _settingsButton.onClick.AddListener(OnSettingsButton);
            _playButton.onClick.AddListener(OnPlayButtonClick);
        }

        private void OnDisable()
        {
            _settingsButton.onClick.RemoveListener(OnSettingsButton);
            _playButton.onClick.RemoveListener(OnPlayButtonClick);
        }

        private void OnPlayButtonClick()
        {
            EventBus<PlayRequestedEvent>.Raise(new PlayRequestedEvent());
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
        }

        private void OnSettingsButton()
        {
            EventBus<OpenSettingsPanelEvent>.Raise(new OpenSettingsPanelEvent());
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
        }

        public void SetLevelText(int level)
        {
            _levelText.text = "Level " + level;
        }

        public static (int a, int b) Convert(int n)
        {
            int a = (n - 1) / 10;
            int b = ((n - 1) % 10) + 1;
            return (a, b);
        }
    }
}
