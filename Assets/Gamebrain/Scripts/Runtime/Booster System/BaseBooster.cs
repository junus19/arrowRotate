using UnityEngine;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public enum BoosterActionWaitType
    {
        Time,
        ActionComplete
    }

    public class BaseBooster : MonoBehaviour
    {
        [SerializeField] protected bool isActive;
        public bool IsActive => isActive;

        protected BoosterManager boosterManager;

        [SerializeField]protected BoosterItemData boosterItemData;
        public BoosterType BoosterType => boosterItemData.BoosterType;

        [SerializeField] bool boosterActionCompleted;
        public bool BoosterActionCompleted => boosterActionCompleted;

        [SerializeField] bool setUIAndCameraForBoosterView;
        [SerializeField] protected GameplayCamera gameplayCamera;

        [SerializeField] protected LayerMask layerMask;
        public LayerMask LayerMask => layerMask;

        protected IGameplayManager _gameplayManager;
        public IGameplayManager GameplayManager => _gameplayManager;
        private EventBinding<BoosterRequestedEvent> boosterRequestedEventBinding;

        public void Init(BoosterManager _boosterManager, BoosterItemData _boosterItemData)
        {
            gameplayCamera = FindAnyObjectByType<GameplayCamera>();
            boosterManager = _boosterManager;
            boosterItemData = _boosterItemData;

            boosterRequestedEventBinding = new EventBinding<BoosterRequestedEvent>(StartBoosterAction);
            EventBus<BoosterRequestedEvent>.Register(boosterRequestedEventBinding);

        }

        protected virtual void UseBooster()
        {
            boosterManager.UseBooster(boosterItemData.BoosterType, 1);
        }

        protected virtual void Update()
        {
            if(IsActive)
            {
                UpdateLogic();
            }
        }

        protected virtual void UpdateLogic()
        {

        }

        protected virtual void StartBoosterAction(BoosterRequestedEvent eventInfo)
        {
            if (eventInfo.BoosterType != boosterItemData.BoosterType)
                return;

            if (setUIAndCameraForBoosterView)
            {
                CameraManager.Instance.GameplayCamera.GetComponent<GameplayCamera>().SetBoosterView(true);
                FindAnyObjectByType<BoosterActiveUI>().Show(boosterItemData);
            }

            isActive = true;
            
            EventBus<BoosterActionStartedEvent>.Raise(new BoosterActionStartedEvent(boosterItemData.BoosterType));
            EventBus<InputLockRequestedEvent>.Raise(new InputLockRequestedEvent());
        }

        protected virtual void ExecuteBoosterAction()
        {
        }

        protected virtual void EndBoosterAction(bool isUsed)
        {
            if (setUIAndCameraForBoosterView)
            {
                CameraManager.Instance.GameplayCamera.GetComponent<GameplayCamera>().SetDefaultView(true);
                FindAnyObjectByType<BoosterActiveUI>().Hide();
            }

            isActive = false;
            EventBus<BoosterActionEndedEvent>.Raise(new BoosterActionEndedEvent(boosterItemData.BoosterType));
            EventBus<InputUnlockRequestedEvent>.Raise(new InputUnlockRequestedEvent());
        }

        private void OnDestroy()
        {
            EventBus<BoosterRequestedEvent>.Deregister(boosterRequestedEventBinding);
        }
    }
}
