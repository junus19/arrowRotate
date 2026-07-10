using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class LevelCompletePanel : UIPanel
    {
        [SerializeField] private Button _returnToMainMenuButton;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private GameObject _levelEndParticle;

        private void OnEnable()
        {
            _returnToMainMenuButton?.onClick.AddListener(OnReturnToMainMenuButtonClick);
            _nextLevelButton.onClick.AddListener(OnNextLevelButtonClick);
                        EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.FireWorks));

        }

        private void OnDisable()
        {
            _returnToMainMenuButton?.onClick.RemoveListener(OnReturnToMainMenuButtonClick);
            _nextLevelButton.onClick.RemoveListener(OnNextLevelButtonClick);
        }

        public void ShowLevelCompleteFx()
        {
            //_levelEndParticle.SetActive(true);
        }

        private void OnReturnToMainMenuButtonClick()
        {
            EventBus<MainMenuRequestedEvent>.Raise(new MainMenuRequestedEvent());
        }

        private void OnNextLevelButtonClick()
        {
            EventBus<NextLevelRequestedEvent>.Raise(new NextLevelRequestedEvent());
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
        }
    }
}
