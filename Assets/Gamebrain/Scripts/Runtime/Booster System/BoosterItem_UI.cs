using TMPro;
using UnityEngine;
using UnityEngine.UI;
using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class BoosterItem_UI : MonoBehaviour
    {
        [SerializeField] BoosterItemData boosterItemData;

        [SerializeField] Image icon;
        [SerializeField] Image iconDisabled;
        [SerializeField] TextMeshProUGUI boosterCountText;
        [SerializeField] TextMeshProUGUI boosterUnlcokLvlText;

        [SerializeField] GameObject lockObject;
        [SerializeField] GameObject unlockedObject;
        [SerializeField] GameObject boosterCountTxtObject;
        [SerializeField] GameObject rewardedObject;
        [SerializeField] GameObject rewardedLoadingObject;

        [SerializeField] Button useButton;
        [SerializeField] Button lockedButton;
        [SerializeField] Button rewardedButton;
        private BoosterManager _boosterManager;
        
        private EventBinding<BoosterCountUpdateEvent> _boosterCountUpdatedEventBinding; // Todo: implement!
        private EventBinding<BoosterActiveStatusEvent> _boosterActiveStatusEventBinding;

        public void Init(bool isActive, BoosterItemData _boosterItemData, int boosterCount, BoosterManager boosterManager)
        {
            _boosterManager = boosterManager;
            boosterItemData = _boosterItemData;

            _boosterCountUpdatedEventBinding = new EventBinding<BoosterCountUpdateEvent>(OnBoosterCountUpdated);

            _boosterActiveStatusEventBinding = new EventBinding<BoosterActiveStatusEvent>(OnBoosterActiveStatusEvent);
            EventBus<BoosterActiveStatusEvent>.Register(_boosterActiveStatusEventBinding);

            boosterUnlcokLvlText.text = "Lvl " + boosterManager.GetBoosterVisualUnlockLevel(boosterItemData.BoosterType);
            if (isActive)
            {
                //UpdateBoosterCount(boosterCount);
                //Unlock();

                ActivateBooster(boosterCount);
            }
            else
            {
                Lock();
            }

            icon.sprite = boosterItemData.Icon;
            gameObject.SetActive(true);
        }

        private void OnEnable()
        {
            EventBus<BoosterCountUpdateEvent>.Register(_boosterCountUpdatedEventBinding);

            useButton.onClick.AddListener(OnUseButton);
            // lockedButton.onClick.AddListener(OnLockedButton);
            rewardedButton.onClick.AddListener(OnRewardedButton);
            rewardedLoadingObject.SetActive(false);
        }

        private void OnDisable()
        {
            EventBus<BoosterCountUpdateEvent>.Deregister(_boosterCountUpdatedEventBinding);
            useButton.onClick.RemoveListener(OnUseButton);
            // lockedButton.onClick.RemoveListener(OnLockedButton);
            rewardedButton.onClick.RemoveListener(OnRewardedButton);
        }

        private void Lock()
        {
            lockObject.SetActive(true);
            iconDisabled.sprite = boosterItemData.IconDisabled;
            unlockedObject.SetActive(false);
        }

        public void ActivateBooster(int boosterCount)
        {
            UpdateBoosterCount(boosterCount);
            Unlock();
        }

        private void Unlock()
        {
            unlockedObject.SetActive(true);
            lockObject.SetActive(false);
        }

        private void UpdateBoosterCount(int count)
        {
            if(count > 0)
            {
                boosterCountText.text = count.ToString();
                if (!boosterCountTxtObject.activeSelf)
                    boosterCountTxtObject.SetActive(true);
                if (rewardedObject.activeSelf)
                    rewardedObject.SetActive(false);
                if (rewardedLoadingObject.activeSelf)
                    rewardedLoadingObject.SetActive(false);
            }
            else
            {
                if (boosterCountTxtObject.activeSelf)
                    boosterCountTxtObject.SetActive(false);
                if (!rewardedObject.activeSelf)
                    rewardedObject.SetActive(true);
            }
        }

        private void OnUseButton()
        {
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
            _boosterManager.TryUseBooster(boosterItemData.BoosterType);
        }

        private void OnRewardedButton()
        {
            EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
            rewardedObject.SetActive(false);
            rewardedLoadingObject.SetActive(true);
            _boosterManager.OnBoosterRewardedRequested(boosterItemData.BoosterType, onFailure:OnRewardedFailed);
        }

        private void OnRewardedFailed()
        {
            rewardedLoadingObject.SetActive(false);
            UpdateBoosterCount(_boosterManager.GetBoosterCount(boosterItemData.BoosterType));
        }

        private void OnLockedButton()
        {

        }
        
        private void OnBoosterCountUpdated(BoosterCountUpdateEvent eventInfo)
        {
            if(eventInfo.BoosterType == boosterItemData.BoosterType)
                UpdateBoosterCount(_boosterManager.GetBoosterCount(boosterItemData.BoosterType));
        }

        private void OnBoosterActiveStatusEvent(BoosterActiveStatusEvent eventInfo)
        {
            if(eventInfo.BoosterType == boosterItemData.BoosterType && eventInfo.IsActive)
            {
                ActivateBooster(eventInfo.BoosterCount);
                //Unlock();eventInfo.
            }
        }
    }
}
