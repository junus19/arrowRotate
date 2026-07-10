using System;
using GameBrain.Utils;
using System.Collections.Generic;
using GameBrain.SDK.Monetization;

namespace GameBrain.Casual
{
    public class BoosterManager
    {
        private readonly BoosterData boosterData;
        private readonly BoosterGameData boosterSaveData;
        private readonly AdService _adService;
        private List<BaseBooster> boosters;

        public BoosterData BoosterData => boosterData;

        public BoosterManager(BoosterData boosterData, BoosterGameData boosterSaveData, AdService adService)
        {
            this.boosterData = boosterData;
            this.boosterSaveData = boosterSaveData;
            boosters = new List<BaseBooster>();
            _adService = adService;
        }

        public void Init()
        {
            foreach (var boosterDataItem in boosterData.BoosterDatas)
            {
                boosterSaveData.AddBoosterData(boosterDataItem.BoosterType);
                if (boosterDataItem.BoosterPrefab != null)
                {
                    var booster = UnityEngine.Object.Instantiate(boosterDataItem.BoosterPrefab);
                    booster.Init(this, boosterDataItem);
                    boosters.Add(booster);
                }
            }
        }

        public int GetBoosterVisualUnlockLevel(BoosterType boosterType)
        {
            return boosterSaveData.GetBoosterUnlcokVisualLevel(boosterType);
        }

        public bool CanUseBooster(BoosterType boosterType)
        {
            return boosterSaveData.GetBoosterCount(boosterType) > 0;
        }

        public void UpdateActiveStatusOfBoosters(int level)
        {
            foreach (var booster in boosters)
            {
                EventBus<BoosterActiveStatusEvent>.Raise(new BoosterActiveStatusEvent(booster.BoosterType, IsBoosterActive(booster.BoosterType, level),
                    boosterSaveData.GetBoosterCount(booster.BoosterType)));
            }
        }

        public void AddBooster(BoosterType boosterType, int amount)
        {
            boosterSaveData.AddBooster(boosterType, amount);
            EventBus<BoosterCountUpdateEvent>.Raise(new BoosterCountUpdateEvent(boosterType, boosterSaveData.GetBoosterCount(boosterType)));
        }

        public void TryUseBooster(BoosterType boosterType)
        {
            if (CanUseBooster(boosterType))
            {
                EventBus<FxRequestEvent>.Raise(new FxRequestEvent(EffectType.Button));
                EventBus<BoosterRequestedEvent>.Raise(new BoosterRequestedEvent(boosterType));
            }
        }

        public void UseBooster(BoosterType boosterType, int amount)
        {
            boosterSaveData.UseBooster(boosterType, amount);
            EventBus<BoosterCountUpdateEvent>.Raise(new BoosterCountUpdateEvent(boosterType, boosterSaveData.GetBoosterCount(boosterType)));
        }

        public int GetBoosterCount(BoosterType boosterType) => boosterSaveData.GetBoosterCount(boosterType);

        public bool IsBoosterActive(BoosterType boosterType, int level) =>
            boosterSaveData.IsBoosterActive(boosterType, level); // Data.BoosterGameDataItems.First(booster => booster._BoosterType == boosterType).IsActive;

        public void OnBoosterRewardedRequested(BoosterType boosterType, Action onSuccess = null, Action onFailure = null)
        {
            EventBus<BoosterRewardedRequestedEvent>.Raise(new BoosterRewardedRequestedEvent(boosterType));
            _adService.ShowRewardedAd
            (
                $"{boosterType.ToString()}_Rewarded",
                () =>
                {
                    AddBooster(boosterType, 1);
                    EventBus<BoosterRewardedCompletedEvent>.Raise(new BoosterRewardedCompletedEvent(boosterType));
                    onSuccess?.Invoke();
                },
                onFailure
            );
        }
    }
}
