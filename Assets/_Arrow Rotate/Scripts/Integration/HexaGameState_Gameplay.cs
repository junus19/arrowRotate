using ArrowRotate.Game;
using ArrowRotate.Generation;
using ArrowRotate.View;
using EC.Core.Common;
using GameBrain.Casual;
using GameBrain.Utils;
using UnityEngine;

namespace ArrowRotate.Integration
{
    /// <summary>
    /// Gamebrain gameplay state'inin Hexa Arrows uyarlaması (arrowJam deseni).
    /// Game sahnesindeki HexaGameplayManager'ı bulur, seed'den level üretir,
    /// manager olaylarını EventBus'a çevirir ve win'i OnLevelFinished'e bağlar.
    /// </summary>
    public class HexaGameState_Gameplay : GameState_Gameplay
    {
        private HexaGameplayManager _gameplayManager;
        private TapController _tapController;

        public HexaGameState_Gameplay(GameStateContext context, BoosterManager boosterManager)
            : base(context, boosterManager, null)
        {
        }

        protected override void OnLevelReady()
        {
            base.OnLevelReady();

            _gameplayManager = Object.FindFirstObjectByType<HexaGameplayManager>();
            if (_gameplayManager == null)
            {
                Debug.LogError("[Hexa] Game sahnesinde HexaGameplayManager yok!");
                return;
            }
            _tapController = _gameplayManager.GetComponent<TapController>();

            StartHexaLevel();
        }

        private void StartHexaLevel()
        {
            var data = _levelManager.CurrentLevel.Data as HexaLevelData;
            if (data == null)
            {
                Debug.LogError($"[Hexa] LevelData HexaLevelData değil: {_levelManager.CurrentLevel.Data.name}");
                return;
            }

            var level = LevelGenerator.Generate(data.Seed, data.BuildConfig());
            _gameplayManager.Begin(level);

            var gameplayCam = CameraManager.Instance.GameplayCamera;
            _gameplayManager.Board.FitCamera(gameplayCam);
            if (_tapController != null) _tapController.Cam = gameplayCam;

            SubscribeGameplay();

            int frozen = 0;
            foreach (var a in level.Arrows) if (a.FreezeAt > 0) frozen++;
            EventBus<HexaLevelStartedEvent>.Raise(new HexaLevelStartedEvent
            {
                ArrowCount = level.Arrows.Count,
                FrozenArrowCount = frozen,
                Seed = level.Seed
            });
            Debug.Log($"[Hexa] Level başladı — seed={level.Seed}, ok={level.Arrows.Count}, buzlu={frozen}");
        }

        private void SubscribeGameplay()
        {
            _gameplayManager.LevelWon += OnHexaWon;
            _gameplayManager.RotatePerformed += OnHexaRotate;
            _gameplayManager.ArrowConnected += OnHexaConnected;
            _gameplayManager.ArrowExited += OnHexaExited;
            _gameplayManager.ArrowBlocked += OnHexaBlocked;
            _gameplayManager.IceBroken += OnHexaIceBroken;
        }

        private void UnsubscribeGameplay()
        {
            if (_gameplayManager == null) return;
            _gameplayManager.LevelWon -= OnHexaWon;
            _gameplayManager.RotatePerformed -= OnHexaRotate;
            _gameplayManager.ArrowConnected -= OnHexaConnected;
            _gameplayManager.ArrowExited -= OnHexaExited;
            _gameplayManager.ArrowBlocked -= OnHexaBlocked;
            _gameplayManager.IceBroken -= OnHexaIceBroken;
        }

        private void OnHexaWon()
        {
            EventBus<HexaLevelWonEvent>.Raise(new HexaLevelWonEvent
            {
                MoveCount = _gameplayManager.MoveCount,
                ElapsedSeconds = _gameplayManager.Elapsed
            });
            OnLevelFinished(Status.Success);
        }

        private void OnHexaRotate(int arrowId)
        {
            EventBus<HexaRotateEvent>.Raise(new HexaRotateEvent { ArrowId = arrowId, MoveCount = _gameplayManager.MoveCount });
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Drag)); // dönüş tıkı (ses anahtarı Faz 7'de özelleşir)
        }

        private void OnHexaConnected(int arrowId)
            => EventBus<HexaArrowConnectedEvent>.Raise(new HexaArrowConnectedEvent { ArrowId = arrowId });

        private void OnHexaExited(int arrowId)
        {
            var level = _gameplayManager.Level;
            EventBus<HexaArrowExitedEvent>.Raise(new HexaArrowExitedEvent
            {
                ArrowId = arrowId,
                ExitedCount = level?.ExitedCount ?? 0,
                TotalCount = level?.Arrows.Count ?? 0
            });
        }

        private void OnHexaBlocked(int arrowId)
            => EventBus<HexaArrowBlockedEvent>.Raise(new HexaArrowBlockedEvent { ArrowId = arrowId });

        private void OnHexaIceBroken(int arrowId)
            => EventBus<HexaIceBrokenEvent>.Raise(new HexaIceBrokenEvent { ArrowId = arrowId });

        protected override void OnLevelFinished(Status status)
        {
            UnsubscribeGameplay();
            base.OnLevelFinished(status);
        }

        protected override void OnExit(State nextState)
        {
            UnsubscribeGameplay();
            base.OnExit(nextState);
        }

        protected override void OnInputLockRequested(InputLockRequestedEvent eventInfo)
        {
            base.OnInputLockRequested(eventInfo);
            if (_gameplayManager != null) _gameplayManager.InputLocked = true;
        }

        protected override void OnInputUnlockRequested(InputUnlockRequestedEvent eventInfo)
        {
            base.OnInputUnlockRequested(eventInfo);
            if (_gameplayManager != null) _gameplayManager.InputLocked = false;
        }
    }
}
