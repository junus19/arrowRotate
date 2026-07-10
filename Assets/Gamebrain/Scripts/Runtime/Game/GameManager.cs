using DG.Tweening;
using UnityEngine;
using GameBrain.SDK;
using EC.Core.Common;
using System.Collections;
using UnityEngine.SceneManagement;

namespace GameBrain.Casual
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-1)]
    public class GameManager : MonoBehaviour
    {
        protected StateMachine _stateMachine;
        protected GameStateBase _mainState;
        protected GameStateBase _inGameState;
        protected GameStateBase _looseState;
        protected GameStateBase _winState;
        protected GameStateBase _restartState;
        protected Camera _mainCamera;
        protected GUIService _guiService;
        protected LevelManager _levelManager;
        protected CurrencyManager _currencyManager;
        protected GameMetaSystem _gameMetaSystem;
        protected Settings _settings;
        protected FeedbackManager _feedbackManager;
        protected BoosterManager _boosterManager;
        protected AnalyticManager _analyticManager;
        protected BoardObjectFactory _boardObjectFactory;
        protected BoardObjectProvider _boardObjectProvider;

        [Header("Data")]
        [SerializeField] protected GameData _gameData;
        [SerializeField] protected GameConfig _gameConfig;
        [SerializeField] protected SettingsData _settingsData;
        [SerializeField] protected Feedbacks_SO _feedbackData;
        [SerializeField] protected GameMetaData _gameMetaData;
        [SerializeField] protected BoosterData _boosterDataBase;
        [SerializeField] protected BoosterGameData _boosterSaveData;
        [SerializeField] protected BoardObjectsDataHolder _boardObjectsDataHolder;

        protected virtual IEnumerator Initialize()
        {
            Application.targetFrameRate = 60;

            // Dependencies!
            _stateMachine = new StateMachine();
            _mainCamera = Camera.main;

            // Scenes!
            yield return SceneManager.LoadSceneAsync("GUI", LoadSceneMode.Additive);
            UnityEngine.SceneManagement.Scene guiScene = SceneManager.GetSceneByName("GUI");
            _guiService = FindFirstObjectByType<GUIService>(FindObjectsInactive.Include);
            _guiService.DisableAllPanels();

            // Manager!
            _analyticManager = FindAnyObjectByType<AnalyticManager>(FindObjectsInactive.Include);
            _gameMetaSystem = new GameMetaSystem(_gameMetaData);
            _levelManager = new LevelManager(_gameConfig.Levels, _gameConfig.Levels);
            _settings = new Settings(_settingsData);
            _feedbackManager = new FeedbackManager(_feedbackData, _settingsData);
            _feedbackManager.Init();
            _boosterManager = new BoosterManager(_boosterDataBase, _boosterSaveData, _analyticManager?.ADService);
            _boosterManager.Init();
            _currencyManager = new CurrencyManager(_gameData);
            _currencyManager.Init();
            _boardObjectFactory = new BoardObjectFactory(_boardObjectsDataHolder.BoardObjectInfos);
            _boardObjectProvider = new BoardObjectProvider(_boardObjectFactory);

            // GUI
            _guiService.SettingsPanel.InitPanel(_settingsData.GetHapticStatus(), _settingsData.GetAudioFxStatus());
            _guiService.GameplayPanel.InitSettingButtons(_settingsData.GetHapticStatus(), _settingsData.GetAudioFxStatus());
            _guiService.GameplayPanel.OnInject(new[] { _boosterManager });
            _guiService.GameplayPanel.SetBoosters(_gameData.GetLevelIndex());

            // States!
            GameStateContext context = new GameStateContext(_stateMachine, _gameData, _gameConfig, _guiService, _levelManager, _analyticManager, _gameMetaSystem, _mainCamera);
            _mainState = new GameState_Main(context);
            _inGameState = new GameState_Gameplay(context, _boosterManager, _boardObjectProvider);
            _looseState = new GameState_Loose(context);
            _winState = new GameState_Win(context);
            _restartState = new GameState_Restart(context);

            // State Transitions!
            _mainState.AddTransition(new Transition(_inGameState, null));
            _inGameState.AddTransition(new Transition(_looseState, null));
            _inGameState.AddTransition(new Transition(_winState, null));
            _inGameState.AddTransition(new Transition(_restartState, null));
            _inGameState.AddTransition(new Transition(_mainState, null));
            _looseState.AddTransition(new Transition(_restartState, null));
            _winState.AddTransition(new Transition(_mainState, null));
            _winState.AddTransition(new Transition(_inGameState, null));
            _restartState.AddTransition(new Transition(_inGameState, null));
            
            OnInitializationCompleted(context);
            
            // Starting the game!
            _stateMachine.Initialize(_mainState);
        }

        protected virtual void Awake() => StartCoroutine(Initialize());
        
        protected virtual void OnInitializationCompleted(GameStateContext context){}

        protected virtual void Update() => _stateMachine?.Update();

        protected virtual void LateUpdate() => _stateMachine?.LateUpdate();

        protected virtual void FixedUpdate() => _stateMachine?.FixedUpdate();

        protected virtual void OnDestroy()
        {
            DOTween.Clear();
            StopAllCoroutines();
        }

        #region SHORTCUT

        internal void TestLevelComplete()
        {
            ((GameState_Gameplay)_inGameState).TestLevelFinished(Status.Success);
        }

        internal void TestLevelFail()
        {
            ((GameState_Gameplay)_inGameState).TestLevelFinished(Status.Fail);
        }

        internal void TestAddBooster()
        {
            _boosterManager.AddBooster(BoosterType.Hammer, 10);
            _boosterManager.AddBooster(BoosterType.Refresh, 10);
            _boosterManager.AddBooster(BoosterType.Swap, 10);
        }

        internal void TestClearData()
        {
            _boosterSaveData.ClearData();
            _gameData.ClearData();
        }
        #endregion
    }
}
