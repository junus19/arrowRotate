using UnityEngine;
using GameBrain.SDK;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class RewardedCellBoardObject : BoardObject
    {
        [SerializeField] private CellLockVisual cellLockVisual;
        [SerializeField] private GameObject _defaultState;
        [SerializeField] private GameObject _loadingState;
        private EventBinding<BoosterActionStartedEvent> _boosterActionStartedEventBinding;
        private EventBinding<BoosterActionEndedEvent> _boosterActionEndedEventBinding;

        private EventBinding<InputLockRequestedEvent> _inputLockRequestedEventBinding;
        private EventBinding<InputUnlockRequestedEvent> _inputUnlockRequestedEventBinding;

        private bool _canInteract = true;

        private void Awake()
        {
            _boosterActionStartedEventBinding = new EventBinding<BoosterActionStartedEvent>(OnBoosterActionStarted);
            _boosterActionEndedEventBinding = new EventBinding<BoosterActionEndedEvent>(OnBoosterActionEnded);
            _inputLockRequestedEventBinding = new EventBinding<InputLockRequestedEvent>(OnInputLocked);
            _inputUnlockRequestedEventBinding = new EventBinding<InputUnlockRequestedEvent>(OnInputUnlocked);

            _defaultState.SetActive(true);
            _loadingState.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus<BoosterActionStartedEvent>.Register(_boosterActionStartedEventBinding);
            EventBus<BoosterActionEndedEvent>.Register(_boosterActionEndedEventBinding);

            EventBus<InputLockRequestedEvent>.Register(_inputLockRequestedEventBinding);
            EventBus<InputUnlockRequestedEvent>.Register(_inputUnlockRequestedEventBinding);
        }

        private void OnDisable()
        {
            EventBus<BoosterActionStartedEvent>.Deregister(_boosterActionStartedEventBinding);
            EventBus<BoosterActionEndedEvent>.Deregister(_boosterActionEndedEventBinding);

            EventBus<InputLockRequestedEvent>.Deregister(_inputLockRequestedEventBinding);
            EventBus<InputUnlockRequestedEvent>.Deregister(_inputUnlockRequestedEventBinding);
        }

        private void OnBoosterActionStarted(BoosterActionStartedEvent eventInfo) => _canInteract = false;

        private void OnBoosterActionEnded(BoosterActionEndedEvent eventInfo) => _canInteract = true;

        private void OnInputLocked(InputLockRequestedEvent eventInfo)
        {
            _canInteract = false;
        }

        private void OnInputUnlocked(InputUnlockRequestedEvent eventInfo)
        {
            _canInteract = true;
        }

        public override void LoadBoardObject(ICell cell, int objectValue)
        {
            currentCell = cell;
            currentCell.LoadBoardObject(this);

            transform.position = currentCell.transform.position;
        }

        public override void UnloadAndDestroyBoardObject()
        {
            currentCell.UnLoadBoardObject();
            cellLockVisual.DestroyAnim();
            //_defaultState.SetActive(false);
            _loadingState.SetActive(false);
            Destroy(gameObject);
        }

        private void OnMouseDown()
        {
            if (!_canInteract) return;
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
            _defaultState.SetActive(false);
            _loadingState.SetActive(true);
            
            if (Application.isEditor) 
                UnloadAndDestroyBoardObject();
            else 
                FindAnyObjectByType<AnalyticManager>()?.ADService?.ShowRewardedAd("Cell_Rewarded", UnloadAndDestroyBoardObject);
        }
    }
}
