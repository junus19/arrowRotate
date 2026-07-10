#if GAMEANALYTICS_ENABLED
using System;
using UnityEngine;
using GameAnalyticsSDK;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GameBrain.SDK
{
    public class GameAnalyticsModule : IAnalyticModule
    {
        private readonly AnalyticsConfig _config;
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;

        public GameAnalyticsModule(AnalyticsConfig config)
        {
            _config = config;
        }
        
        public async Task Initialize()
        {
            try
            {
                Debug.Log("Initializing GameAnalytics...");

                GameAnalytics.onInitialize += OnInitialized;
                GameAnalytics.Initialize();

                while (!_isInitialized)
                {
                    await Task.Delay(100);
                }
                
                Debug.Log("GameAnalytics initialized successfully");
            }
            catch (Exception exception)
            {
                Debug.Log($"GameAnalytics initialization exception: {exception.Message}");
            }
        }

        private void OnInitialized(object sender, bool e)
        {
            _isInitialized = true;
            GameAnalytics.onInitialize -= OnInitialized;
        }

        public void SendCustomEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized) return;

            GameAnalytics.NewDesignEvent(eventName, parameters);
            // Debug.Log($"Event tracked: {eventName}");
        }

        public void SendLevelStartEvent(int level, string levelName = "")
        {
            if (!_isInitialized) return;

            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Start,$"Level_{level}");
            // Debug.Log($"Level start tracked: {level} - {levelName}");
        }

        public void SendLevelCompleteEvent(int level, string levelName = "", float completionTime = 0f)
        {
            if (!_isInitialized) return;

            GameAnalytics.NewProgressionEvent(GAProgressionStatus.Complete, $"Level_{level}", (int)completionTime);
            // Debug.Log($"Level complete tracked: {level} - {levelName}");
        }

        public void SendLevelFailEvent(int level, FailType failType, string levelName = "", string failReason = "", float levelDuration = 0f)
        {
            if (!_isInitialized) return;

            if (failType == FailType.Soft)
                GameAnalytics.NewDesignEvent("SoftFail:Level " + level);
            else if (failType == FailType.Hard)
                GameAnalytics.NewProgressionEvent(GAProgressionStatus.Fail, $"Level_{level}", (int)levelDuration);
            // Debug.Log($"Level fail tracked: {level} - {levelName} ({levelDuration}s)");
        }

        public void SendPurchaseEvent(string itemId, string currency, float amount, string transactionId = "")
        {
            if (!_isInitialized) return;

            GameAnalytics.NewBusinessEvent(currency, (int)(amount * 100), itemId, "shop", transactionId);
            // Debug.Log($"Purchase tracked: {itemId}");
        }

        public void SendAdImpressionEvent(AdType adType, string adPlacement, string adNetwork = "")
        {
            if (!_isInitialized) return;
            
            GAAdType gaAdType = adType switch
            {
                 AdType.Banner => GAAdType.Banner,
                 AdType.Interstitial => GAAdType.Interstitial,
                 AdType.Rewarded => GAAdType.RewardedVideo,
                _ => GAAdType.Undefined
            };

            GameAnalytics.NewAdEvent(GAAdAction.Show, gaAdType, adNetwork, adPlacement);
            // Debug.Log($"Ad impression tracked: {adType}");
        }

        public void SendAdClickEvent(string adPlacement, string adNetwork = "")
        {
            if (!_isInitialized) return;

            GameAnalytics.NewAdEvent(GAAdAction.Clicked, GAAdType.RewardedVideo, adNetwork, adPlacement);
            // Debug.Log($"Ad click tracked: {adType}");
        }

        public void SendResourceEvent(ResourceFlowType flowType, string currency, float amount, string resourceType, string resourceId)
        {
            if (!_isInitialized) return;

            GAResourceFlowType gaResourceFlowType = flowType switch
            {
                ResourceFlowType.Gain => GAResourceFlowType.Source,
                ResourceFlowType.Spend => GAResourceFlowType.Sink,
                _ => GAResourceFlowType.Undefined
            };
            GameAnalytics.NewResourceEvent(gaResourceFlowType, currency, amount, resourceType, resourceId);
            // Debug.Log($"Resource tracked: {resourceType} - {amount} - {flowType}");
        }

        public void SendError(string errorMessage, string errorType = "")
        {
            if (!_isInitialized) return;

            GameAnalytics.NewErrorEvent(GAErrorSeverity.Error, errorMessage);
            // Debug.Log($"Error tracked: {errorType}");
        }

        public void SetUserProperty(string propertyName, string value)
        {
            if (!_isInitialized) return;

            // GameAnalytics uses custom dimensions for user properties
            // Debug.Log($"User property set: {propertyName} = {value}");
        }

        public void SetUserId(string userId)
        {
            if (!_isInitialized) return;

            GameAnalytics.SetCustomId(userId);
            // Debug.Log($"User ID set: {userId}");
        }
    }
}
#endif
