using EC.Utils;
using UnityEngine;
using System.Linq;
using GameBrain.SDK;
using EC.Core.Common;
using GameBrain.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace GameBrain.Casual
{
    public class GameState_Gameplay : GameStateBase
    {
        protected readonly CoroutineHandler _coroutineHandler;

        // Gameplay
        protected readonly EventBinding<RestartRequestedEvent> _restartRequestedEventBinding;
        protected readonly EventBinding<MainMenuRequestedEvent> _mainMenuRequestedEventBinding;
        protected readonly EventBinding<ScoreUpdateEvent> _scoreUpdateEventBinding;
        protected readonly EventBinding<BoardObjectBrokenEvent> _boardObjectBrokenEventBinding;
        protected readonly EventBinding<ReviveRequestedEvent> _reviveRequestedEventBinding;
        protected readonly EventBinding<ReviveDeclinedEvent> _reviveDeclinedEventBinding;

        protected GameState_Main _mainState;
        protected GameState_Restart _restartState;
        protected GameState_Loose _looseState;
        protected GameState_Win _winState;
        protected GameplayCamera _gameplayCamera;
        // protected IGameplayManager _gameplayManager;

        protected readonly Camera _mainCamera;
        protected readonly GameMetaSystem _gameMetaSystem;
        protected List<LevelGoal> _levelGoals = new List<LevelGoal>();
        protected readonly BoardObjectProvider _boardObjectProvider;
        private Tutorial _tutorial;

        // Booster
        protected readonly EventBinding<BoosterActionStartedEvent> _boosterActionStartedEventBinding;
        protected readonly EventBinding<BoosterActionEndedEvent> _boosterActionEndedEventBinding;
        protected readonly BoosterManager _boosterManager;

        // Input
        protected readonly EventBinding<InputLockRequestedEvent> _inputLockRequestedEventBinding;
        protected readonly EventBinding<InputUnlockRequestedEvent> _inputUnlockRequestedEventBinding;

        // Analytic
        protected readonly AnalyticManager _analyticManager;

        public GameState_Gameplay(GameStateContext context, BoosterManager boosterManager, BoardObjectProvider boardObjectProvider) : base(context)
        {
            _gameMetaSystem = context.GameMetaSystem;
            _mainCamera = context.MainCamera;
            _analyticManager = context.AnalyticManager;
            _coroutineHandler = new CoroutineHandler();
            _boosterManager = boosterManager;
            _boardObjectProvider = boardObjectProvider;
            // Event Bindings!
            _restartRequestedEventBinding = new EventBinding<RestartRequestedEvent>(OnRestartRequested);
            _mainMenuRequestedEventBinding = new EventBinding<MainMenuRequestedEvent>(OnMainMenuRequested);
            _scoreUpdateEventBinding = new EventBinding<ScoreUpdateEvent>(OnScoreUpdated);
            _boardObjectBrokenEventBinding = new EventBinding<BoardObjectBrokenEvent>(OnBoardObjectBroken);
            _boosterActionStartedEventBinding = new EventBinding<BoosterActionStartedEvent>(OnBoosterActionStarted);
            _boosterActionEndedEventBinding = new EventBinding<BoosterActionEndedEvent>(OnBoosterActionEnded);
            _inputLockRequestedEventBinding = new EventBinding<InputLockRequestedEvent>(OnInputLockRequested);
            _inputUnlockRequestedEventBinding = new EventBinding<InputUnlockRequestedEvent>(OnInputUnlockRequested);
            _reviveRequestedEventBinding = new EventBinding<ReviveRequestedEvent>(OnReviveRequested);
            _reviveDeclinedEventBinding = new EventBinding<ReviveDeclinedEvent>(OnReviveDeclined);
        }

        protected override void OnEnter(State previousState)
        {
            Debug.Log("On Enter Gameplay State!");

            _mainState ??= (GameState_Main)_transitions.First(transition => transition.TargetState is GameState_Main).TargetState;
            _restartState ??= (GameState_Restart)_transitions.First(transition => transition.TargetState is GameState_Restart).TargetState;
            _looseState ??= (GameState_Loose)_transitions.First(transition => transition.TargetState is GameState_Loose).TargetState;
            _winState ??= (GameState_Win)_transitions.First(transition => transition.TargetState is GameState_Win).TargetState;

            EventBus<RestartRequestedEvent>.Register(_restartRequestedEventBinding);
            EventBus<MainMenuRequestedEvent>.Register(_mainMenuRequestedEventBinding);
            EventBus<ScoreUpdateEvent>.Register(_scoreUpdateEventBinding);
            EventBus<BoardObjectBrokenEvent>.Register(_boardObjectBrokenEventBinding);
            EventBus<BoosterActionStartedEvent>.Register(_boosterActionStartedEventBinding);
            EventBus<BoosterActionEndedEvent>.Register(_boosterActionEndedEventBinding);
            EventBus<InputLockRequestedEvent>.Register(_inputLockRequestedEventBinding);
            EventBus<InputUnlockRequestedEvent>.Register(_inputUnlockRequestedEventBinding);
            EventBus<ReviveRequestedEvent>.Register(_reviveRequestedEventBinding);
            EventBus<ReviveDeclinedEvent>.Register(_reviveDeclinedEventBinding);

            _guiService.LevelGoalPanel.EnableLevelGoalPanel();
            _guiService.LevelGoalPanel.LevelObjectivesContainerUI.Init();
            _guiService.GameplayPanel.HideGameplayItemsCanvasGroupOnStart();

            _boosterManager.UpdateActiveStatusOfBoosters(_gameData.GetLevelIndex());
            _coroutineHandler.StartCoroutine(InitializeLevel());
        }

        protected override void OnExit(State nextState)
        {
            EventBus<RestartRequestedEvent>.Deregister(_restartRequestedEventBinding);
            EventBus<MainMenuRequestedEvent>.Deregister(_mainMenuRequestedEventBinding);
            EventBus<ScoreUpdateEvent>.Deregister(_scoreUpdateEventBinding);
            EventBus<BoardObjectBrokenEvent>.Deregister(_boardObjectBrokenEventBinding);
            EventBus<BoosterActionStartedEvent>.Deregister(_boosterActionStartedEventBinding);
            EventBus<BoosterActionEndedEvent>.Deregister(_boosterActionEndedEventBinding);
            EventBus<InputLockRequestedEvent>.Deregister(_inputLockRequestedEventBinding);
            EventBus<InputUnlockRequestedEvent>.Deregister(_inputUnlockRequestedEventBinding);
            EventBus<ReviveRequestedEvent>.Deregister(_reviveRequestedEventBinding);
            EventBus<ReviveDeclinedEvent>.Deregister(_reviveDeclinedEventBinding);

            // _gameplayManager.OnLevelComplete -= OnComplete;
            // _gameplayManager.OnLevelFail -= OnSoftFail;

            _guiService.LevelGoalPanel.DisableLevelGoalPanel();
            Debug.Log("On Exit Gameplay State!");
        }

        protected IEnumerator InitializeLevel()
        {
            yield return SceneManager.LoadSceneAsync("Game", LoadSceneMode.Additive);
            SceneManager.SetActiveScene(SceneManager.GetSceneByName("Game"));

            if (_gameMode == GameMode.Test)
                yield return _levelManager.LoadLevel(_levelManager.CreateLevel(_testLevel, null));
            else
                yield return _levelManager.LoadLevel(_gameData);// _gameData.GetLevelIndex(), _gameData.RandomLevelLoopStartIndex);

            _mainCamera.gameObject.SetActive(false);
            _guiService.GameplayPanel.SetLevelText(_gameData.GetVisualLevelIndex().ToString());
            _guiService.GameplayPanel.SetActive(true);
            _guiService.GameplayPanel.ShowGameplayItemsCanvasGroupOnStart(false);
            _levelGoals.Clear();
            List<LevelGoalDataItem> levelGoalDataItems = _levelManager.CurrentLevel.Data.Goals;

            foreach (LevelGoalDataItem levelGoalData in levelGoalDataItems)
            {
                if (levelGoalData.GoalType == LevelGoalType.Score)
                    _levelGoals.Add(new LevelGoalDirectSet(levelGoalData.GoalType, levelGoalData.Amount));
                else
                    _levelGoals.Add(new LevelGoalDecrease(levelGoalData.GoalType, levelGoalData.Amount));
            }

            OnLevelReady();
        }

        protected virtual void OnLevelReady()
        {
            // _gameplayManager = SceneManager.GetActiveScene().GetRootGameObjects().Select(rootObject => rootObject.GetComponent<IGameplayManager>()).First(component => component != null);
            CameraManager.Instance.GameplayCamera.gameObject.SetActive(true);
            StartLevel();
        }

        protected virtual void StartLevel()
        {
            _levelManager.CurrentLevel.OnFinish += OnLevelFinished;
            _levelManager.CurrentLevel.Start();

            _levelGoals = new List<LevelGoal>();
            _guiService.LevelGoalPanel.Canvas.renderMode = RenderMode.ScreenSpaceCamera;
            _guiService.LevelGoalPanel.Canvas.worldCamera = CameraManager.Instance.GameplayUICamera;
            _guiService.LevelGoalPanel.Canvas.planeDistance = 8;
            _guiService.LevelGoalPanel.LevelObjectivesContainerUI.SetReadyForLevel();

            foreach (LevelGoalDataItem item in _levelManager.CurrentLevel.Data.Goals)
            {
                if (item.GoalType == LevelGoalType.Score)
                    _levelGoals.Add(new LevelGoalDirectSet(item.GoalType, item.Amount));
                else
                    _levelGoals.Add(new LevelGoalDecrease(item.GoalType, item.Amount));
            }

            //Set score objective at the index 0. 
            for (int i = 1; i < _levelGoals.Count; i++)
            {
                if (_levelGoals[i].GoalType == LevelGoalType.Score)
                {
                    (_levelGoals[0], _levelGoals[i]) = (_levelGoals[i], _levelGoals[0]);
                }
            }

            // // Setting objectives for gui.
            foreach (LevelGoal goal in _levelGoals)
            {
                // There is no sprite! Disabled!
                _guiService.LevelGoalPanel.LevelObjectivesContainerUI.SetObjective(null, goal);
                if (goal.GoalType == LevelGoalType.Score)
                    _guiService.LevelGoalPanel.LevelObjectivesContainerUI.SetObjectiveScore(Resources.Load<Sprite>("Sprites/GoalTileIconRandom"), goal);
            }

            _guiService.LevelGoalPanel.LevelObjectivesContainerUI.ShowObjectives();
            //_guiService.GameplayPanel.ShowGameplayItemsCanvasGroupOnStart(true);
            SetInputLocked(false);
            _analyticManager?.AnalyticsService.SendLevelStartEvent(_gameData.GetAnalyticLevelIndex());

            if(_tutorial != null)
                _tutorial.StartShowingTutorialHand();
        }

        protected virtual void OnScoreUpdated(ScoreUpdateEvent eventInfo)
        {
            //_guiService.GameplayPanel.SetScoreText(eventInfo.score, eventInfo.scoreGoal);
            LevelGoal levelGoal = _levelGoals.Find(x => x.GoalType == LevelGoalType.Score);

            if (levelGoal == null) return;

            levelGoal.UpdateCurrentGoal(eventInfo.score, eventInfo.cellPositionToGiveScore, eventInfo.towerColor);

            CheckLevelStatus();
        }

        private void OnBoardObjectBroken(BoardObjectBrokenEvent eventInfo)
        {
            LevelGoalType levelGoalType = LevelGoalType.None;
            if (eventInfo.BoardObjectType == BoardObjectType.Wood)
                levelGoalType = LevelGoalType.Wood;
            else if (eventInfo.BoardObjectType == BoardObjectType.Ice)
                levelGoalType = LevelGoalType.Ice;
            else if (eventInfo.BoardObjectType == BoardObjectType.Clay)
                levelGoalType = LevelGoalType.Clay;


            var levelGoal = _levelGoals.Find(x => x.GoalType == levelGoalType);

            if (levelGoal != null)
                levelGoal.UpdateCurrentGoal(1, eventInfo.boardObjectPosition, default);

            CheckLevelStatus();

            //var boardObjectGoals = levelGoals.Where(x => x.GoalType != LevelGoalType.Score).ToList();

            //if(boardObjectGoals.Count > 0)
        }

        #region Booster

        protected virtual void OnBoosterActionStarted(BoosterActionStartedEvent eventInfo)
        {
            if (eventInfo.BoosterType == BoosterType.Refresh) return;
            _guiService.GameplayPanel.HideGameplayItemsOnBooster();
            _guiService.LevelGoalPanel.HideLevelGoalPanel();
        }

        protected virtual void OnBoosterActionEnded(BoosterActionEndedEvent eventInfo)
        {
            _guiService.GameplayPanel.ShowGameplayItemsOnBooster();
            _guiService.LevelGoalPanel.ShowLevelGoalPanel();
        }

        #endregion

        #region Input

        public void SetInputLocked(bool locked)
        {
            if (locked) OnInputLockRequested(new InputLockRequestedEvent());
            else OnInputUnlockRequested(new InputUnlockRequestedEvent());
        }

        protected virtual void OnInputLockRequested(InputLockRequestedEvent eventInfo)
        {
            // _gameplayManager.SetInteractable(false);
        }

        protected virtual void OnInputUnlockRequested(InputUnlockRequestedEvent eventInfo)
        {
            // _gameplayManager.SetInteractable(true);
        }

        #endregion

        #region Level

        private void OnComplete() => OnLevelFinished(Status.Success);

        protected void OnSoftFail()
        {
            // _gameplayManager.SetInteractable(false);
            _analyticManager?.AnalyticsService.SendLevelFailEvent(_gameData.GetAnalyticLevelIndex(), FailType.Soft);
            EventBus<InputLockRequestedEvent>.Raise(new InputLockRequestedEvent());
            _revivePanelCoroutine = _coroutineHandler.StartCoroutine(DelayedRevivePanel(1.25f));
        }
        
        private CoroutineHandle _revivePanelCoroutine;

        protected virtual IEnumerator DelayedRevivePanel(float delay = 0f)
        {
            yield return new WaitForSeconds(delay);
            _guiService.GameplayPanel.RevivePopUp.SetActive(true);
            _revivePanelCoroutine = null;
        }

        private void OnReviveRequested()
        {
            _analyticManager?.AnalyticsService.SendAdClickEvent("Revive_rv", "applovin_max");
            _analyticManager?.ADService.ShowRewardedAd("Revive_Rewarded", OnRevive);
        }

        protected virtual void OnRevive()
        {
            EventBus<InputUnlockRequestedEvent>.Raise(new InputUnlockRequestedEvent());
            // _gameplayManager.Revive();
            //_gameplayManager.SetInteractable(true);
            _guiService.GameplayPanel.RevivePopUp.SetActive(false);
            _analyticManager?.AnalyticsService.SendCustomEvent($"Level_{_gameData.GetAnalyticLevelIndex()}:Rewarded:Revive");
            _analyticManager?.AnalyticsService.SendAdImpressionEvent(AdType.Rewarded, "Revive_rv", "applovin_max");
        }

        private void OnReviveDeclined()
        {
            OnLevelFinished(Status.Fail);
        }

        protected virtual void CheckLevelStatus()
        {
            if (_levelGoals.Any(goal => !goal.IsAchieved))
                return;
            OnLevelFinished(Status.Success);
        }

        protected virtual void OnLevelFinished(Status status)
        {
            if (_levelManager.CurrentLevel.Status is not Status.NotCompleted) return;
            Debug.Log($"On LevelFinished {status}");
            OnInputLockRequested(null);
            // _gameplayManager.AbandonShape();
            _levelManager.CurrentLevel.OnFinish -= OnLevelFinished;
            _levelManager.CurrentLevel.Finish(status);
            EventBus<InputLockRequestedEvent>.Raise(new InputLockRequestedEvent());
            if (status == Status.Success)
                _levelCompleteCoroutineHandle = _coroutineHandler.StartCoroutine(DelayedLevelCompleteRoutine());
            else if (status == Status.Fail)
                OnLevelFailed();
        }

        private CoroutineHandle _levelCompleteCoroutineHandle;
        protected virtual IEnumerator DelayedLevelCompleteRoutine()
        {
            // yield return new WaitForSeconds(1.0f);
            _guiService.GameplayPanel.ShowLevelCompleteFx();
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.LevelComplete));
            yield return new WaitForSeconds(0.75f);
            OnLevelCompleted();
            _levelCompleteCoroutineHandle = null;
        }

        protected virtual void OnLevelFailed()
        {
            _analyticManager?.AnalyticsService.SendLevelFailEvent(_gameData.GetAnalyticLevelIndex(), FailType.Hard);
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Fail));
            _stateMachine.ChangeState(_looseState);
        }

        protected virtual void OnLevelCompleted()
        {
            _analyticManager?.AnalyticsService.SendLevelCompleteEvent(_gameData.GetAnalyticLevelIndex());
            _stateMachine.ChangeState(_winState);
        }

        #endregion

        #region Gameplay

        protected virtual void OnRestartRequested()
        {
            if (_levelCompleteCoroutineHandle != null)
                _coroutineHandler.StopCoroutine(_levelCompleteCoroutineHandle);
            if (_revivePanelCoroutine != null)
                _coroutineHandler.StopCoroutine(_revivePanelCoroutine);
            _analyticManager?.AnalyticsService.SendCustomEvent("Restart:Level_" + _gameData.GetAnalyticLevelIndex());
            _stateMachine.ChangeState(_restartState);
        }

        protected virtual void OnMainMenuRequested()
        {
            if (_levelCompleteCoroutineHandle != null)
                _coroutineHandler.StopCoroutine(_levelCompleteCoroutineHandle);
            if (_revivePanelCoroutine != null)
                _coroutineHandler.StopCoroutine(_revivePanelCoroutine);
            
            _analyticManager?.AnalyticsService.SendCustomEvent("Abondoned:Level_" + _gameData.GetAnalyticLevelIndex());
            _levelManager.CurrentLevel.Unload();
            SceneManager.UnloadSceneAsync("Game");
            _stateMachine.ChangeState(_mainState);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            if (_levelManager.CurrentLevel == null) return;
            if (_levelManager.CurrentLevel.Status is Status.Success or Status.Fail) return;
            if (Input.GetKeyDown(KeyCode.Space)) OnLevelFinished(Status.Success);
            if (Input.GetKeyDown(KeyCode.F)) OnLevelFinished(Status.Fail);
        }

        internal void TestLevelFinished(Status success)
        {
            OnLevelFinished(success);
        }

        #endregion
    }
}
