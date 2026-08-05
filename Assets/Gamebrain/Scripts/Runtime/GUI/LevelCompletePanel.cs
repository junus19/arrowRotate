using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;
using TMPro;

namespace GameBrain.Casual
{
    public class LevelCompletePanel : UIPanel
    {
        [SerializeField] private Button _returnToMainMenuButton;
        [SerializeField] private Button _doubleRewardRVButton;
        [SerializeField] private Button _nextLevelButton;
        [SerializeField] private Button _closeButton;
        [SerializeField] private TMP_Text _rewardText;
        [SerializeField] private GameObject _levelEndParticle;
        private GameData _gameData;

        public override void OnInject(object[] args)
        {
            base.OnInject(args);
            _gameData = args[0] as GameData;
        }

        private void OnEnable()
        {
            _returnToMainMenuButton?.onClick.AddListener(OnReturnToMainMenuButtonClick);
            _nextLevelButton.onClick.AddListener(OnNextLevelButtonClick);
            _doubleRewardRVButton.onClick.AddListener(OnDoubleRewardRVButtonClick);
            _closeButton?.onClick.AddListener(OnReturnToMainMenuButtonClick);
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.FireWorks));
            _rewardText.text = _gameData.LevelCompleteReward.ToString();
        }

        private void OnDisable()
        {
            _returnToMainMenuButton?.onClick.RemoveListener(OnReturnToMainMenuButtonClick);
            _nextLevelButton.onClick.RemoveListener(OnNextLevelButtonClick);
            _doubleRewardRVButton.onClick.RemoveListener(OnDoubleRewardRVButtonClick);
            _closeButton?.onClick.RemoveListener(OnReturnToMainMenuButtonClick);
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

        private void OnDoubleRewardRVButtonClick()
        {
            EventBus<DoubleRewardRequestedEvent>.Raise(new DoubleRewardRequestedEvent());
        }
    }
}
