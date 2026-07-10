using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class GameplayPanel : UIPanel
    {
        [SerializeField] private Canvas canvas;
        public Canvas Canvas => canvas;

        [SerializeField] private CanvasGroup _gameplayItemsCanvasGroup;
        [SerializeField] private RevivePopUp _revivePopUp;
        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private Image _scoreFill;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private GameObject _levelEndParticle;
        
        [SerializeField] private bool _isFloatingSettingsActive;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Transform _floatingSettingsParent;
        [SerializeField] private Transform _settingsOnPoint;
        [SerializeField] private Transform _settingsOffPoint;
        [SerializeField] private Image _settingsDarkBg;
        [SerializeField] private Color _settingsDarkBgActiveColor;
        [SerializeField] private Color _settingsDarkBgDeactiveColor;
        [SerializeField] private FloatingSettingButton _hapticButton;
        [SerializeField] private FloatingSettingButton _audioButton;
        [SerializeField] private HomeButton _homeButton;
        [SerializeField] private RestartButton _restartButton;
        [SerializeField] private DeselectModule _settingsDeselectModule;

        //[SerializeField] private LevelObjectivesContainerUI _levelObjectivesContainerUI;
        //public LevelObjectivesContainerUI LevelObjectivesContainerUI => _levelObjectivesContainerUI;
        public RevivePopUp RevivePopUp => _revivePopUp;

        [Header("Booster")]
        [SerializeField] private GameObject _boosterItemsParent;

        [SerializeField] private BoosterItem_UI _boosterItemUIPrefab;
        [SerializeField] private BoosterItem_UI[] _boosterButtons;

        private BoosterManager _boosterManager;

        [Header("Tutorial")]
        [SerializeField] StageUI _tutorialStageUI;
        [SerializeField] private GameObject _tutorialHand;
        public GameObject TutorialHand => _tutorialHand;

        public StageUI TutorialStageUI => _tutorialStageUI;

        public override void OnInject(object[] args)
        {
            base.OnInject(args);
            _boosterManager = args[0] as BoosterManager;
        }

        private void OnEnable()
        {
            _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            _settingsDeselectModule.OnDeselected += CloseFloatingSettingsParent;


            MoveFloatingSettingParent(false, false);
            HideBoosters();
        }

        private void OnDisable()
        {
            _settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            _settingsDeselectModule.OnDeselected -= CloseFloatingSettingsParent;
            MoveFloatingSettingParent(false, false);
        }

        public void HideGameplayItemsCanvasGroupOnStart()
        {
            _isFloatingSettingsActive = false;
            _settingsDeselectModule.enabled = false;

            MoveFloatingSettingParent(false, false);
            _gameplayItemsCanvasGroup.alpha = 0;
            //_levelObjectivesContainerUI.mainCanvasGroup.enabled = true;
            _levelEndParticle.SetActive(false);
        }

        public void ShowGameplayItemsCanvasGroupOnStart(bool isFadeoutPanelActive)
        {
            //float delay = isFadeoutPanelActive ? 2.0f : 0.0f;

            _gameplayItemsCanvasGroup.DOKill();
            _gameplayItemsCanvasGroup.DOFade(1.0f, .5f).SetDelay(1f);//SetDelay(delay + 2.2f);
        }

        public void SetLevelText(string level) => _levelText.text = "level "+level;//"level\n" + level;

        public void SetScore(int currentScore, int totalScore)
        {
            _scoreText.text = Mathf.Min(currentScore, totalScore) + "/" + totalScore;

            if (currentScore >= totalScore)
            {
                _levelEndParticle.SetActive(true);
                _scoreFill.fillAmount = 1.0f;
            }
            else
            {
                float fill = currentScore / (float)totalScore;
                _scoreFill.fillAmount = fill;
            }
        }

        public void SetScoreText(int currentScore, int totalScore)
        {
            _scoreText.text = Mathf.Min(currentScore, totalScore) + "/" + totalScore;

            
        }

        public void SetBoosters(int level)
        {
            _boosterButtons = new BoosterItem_UI[_boosterManager.BoosterData.BoosterDatas.Count];
            for (int index = 0; index < _boosterManager.BoosterData.BoosterDatas.Count; index++)
            {
                BoosterItem_UI boosterItemUI = Instantiate(_boosterItemUIPrefab, _boosterItemsParent.transform);
                _boosterButtons[index] = boosterItemUI;
                BoosterItemData boosterItemData = _boosterManager.BoosterData.BoosterDatas[index];
                bool isActive = GetStatusOfBoosters(boosterItemData, level);
                int boosterCount = _boosterManager.GetBoosterCount(boosterItemData.BoosterType);
                boosterItemUI.Init(isActive, boosterItemData, boosterCount, _boosterManager);
            }
        }

        public bool GetStatusOfBoosters(BoosterItemData boosterItemData, int level)
        {
            return _boosterManager.IsBoosterActive(boosterItemData.BoosterType, level);
        }

        public void InitSettingButtons(bool hapticStatus, bool audioStatus)
        {
            _hapticButton.Init(hapticStatus);
            _audioButton.Init(audioStatus);
        }

        public void OnSettingsButtonClicked()
        {
            _isFloatingSettingsActive = !_isFloatingSettingsActive;
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));

            if (_isFloatingSettingsActive)
            {
                _settingsDeselectModule.enabled = true;
                _settingsDeselectModule.SetSelected();
                EventBus<InputLockRequestedEvent>.Raise(new InputLockRequestedEvent());
            }
            //else
            //  EventBus<InputUnlockRequestedEvent>.Raise(new InputUnlockRequestedEvent());

            _settingsDarkBg.color = _settingsDarkBgDeactiveColor;
            _settingsDarkBg.enabled = true;
            MoveFloatingSettingParent(_isFloatingSettingsActive, true);
        }

        private void MoveFloatingSettingParent(bool status, bool withAnim)
        {
            Vector3 floatingSettingsParentTarget = status ? _settingsOnPoint.position : _settingsOffPoint.position;
            if (status)
                _settingsDarkBg.DOColor(_settingsDarkBgActiveColor, 0.3f);
            else
                _settingsDarkBg.DOColor(_settingsDarkBgDeactiveColor, 0.3f).OnComplete(() =>
                {
                    _settingsDarkBg.enabled = false;
                    EventBus<InputUnlockRequestedEvent>.Raise(new InputUnlockRequestedEvent());
                });

            if (withAnim)
                _floatingSettingsParent.DOMove(floatingSettingsParentTarget, 0.3f).SetEase(Ease.OutCubic);
            else
                _floatingSettingsParent.position = floatingSettingsParentTarget;
        }

        private void CloseFloatingSettingsParent()
        {
            _isFloatingSettingsActive = false;
            _settingsDeselectModule.enabled = false;

            MoveFloatingSettingParent(_isFloatingSettingsActive, true);
        }

        public void ShowGameplayItemsOnBooster()
        {
            _gameplayItemsCanvasGroup.blocksRaycasts = true;
            _gameplayItemsCanvasGroup.DOKill();
            _gameplayItemsCanvasGroup.DOFade(1f, .5f);
        }

        public void HideGameplayItemsOnBooster()
        {
            //_levelObjectivesContainerUI.mainCanvasGroup.enabled = false;

            _gameplayItemsCanvasGroup.blocksRaycasts = false;
            _gameplayItemsCanvasGroup.DOKill();
            _gameplayItemsCanvasGroup.DOFade(0f, .5f);
        }

        public void ShowLevelCompleteFx()
        {
            _levelEndParticle.SetActive(true);
        }

        public void ShowTutorialUI() => _tutorialStageUI.gameObject.SetActive(true);

        public void HideTutorialUI() => _tutorialStageUI.gameObject.SetActive(false);

        public void ShowBoosters()
        {
            _boosterItemsParent.SetActive(true);
        }

        public void HideBoosters()
        {
            _boosterItemsParent.SetActive(false);
        }
    }
}
