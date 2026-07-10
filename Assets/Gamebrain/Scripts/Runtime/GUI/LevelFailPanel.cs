using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class LevelFailPanel : UIPanel
    {
        [SerializeField] private Button _returnToMainMenuButton;

        private void OnEnable()
        {
            _returnToMainMenuButton.onClick.AddListener(OnReturnToMainMenuButtonClick);
        }

        private void OnDisable()
        {
            _returnToMainMenuButton.onClick.RemoveListener(OnReturnToMainMenuButtonClick);
        }

        private void OnReturnToMainMenuButtonClick()
        {
            EventBus<MainMenuRequestedEvent>.Raise(new MainMenuRequestedEvent());
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
        }
    }
}
