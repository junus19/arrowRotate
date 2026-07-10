using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class MainMenuSettingsPanel : UIPanel
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private Button _fullScreenButton;
        [SerializeField] private SettingsItemButton _hapticButton;
        [SerializeField] private SettingsItemButton _soundButton;

        protected override void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseButton);
        }

        private void OnCloseButton()
        {
            SetActive(false);
        }
    }
}
