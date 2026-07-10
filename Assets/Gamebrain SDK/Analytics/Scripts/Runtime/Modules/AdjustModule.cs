using System;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

#if ADJUST_ENABLED
using AdjustSdk;
using Facebook.Unity.Settings;

namespace GameBrain.SDK
{
    public class AdjustModule : IAnalyticModule
    {
        private bool _isInitialized = false;
        private readonly AnalyticsConfig _config;
        
        public bool IsInitialized => _isInitialized;

        public AdjustModule(AnalyticsConfig config)
        {
            _config = config;
        }
        
        public async Task Initialize()
        {
            try
            {
                Debug.Log("Initializing Adjust SDK...");
                AdjustSdk.AdjustEnvironment environment = _config.AdjustEnvironment == AdjustEnvironment.Production
                    ? AdjustSdk.AdjustEnvironment.Production
                    : AdjustSdk.AdjustEnvironment.Sandbox;
                AdjustConfig adjustConfig = new AdjustConfig(_config.AdjustAppToken, environment)
                {
                    FbAppId = FacebookSettings.AppId,
                    AttributionChangedDelegate = HandleAttribution
                };
                Adjust.InitSdk(adjustConfig);
                Adjust.GetAttribution(HandleAttribution);

                await Task.Delay(500);

                _isInitialized = true;
                Debug.Log("Adjust SDK initialized successfully.");
            }
            catch (Exception exception)
            {
                Debug.Log($"Adjust SDK initialization exception: {exception.Message}");
            }
        }

        private void HandleAttribution(AdjustAttribution attribution)
        {
            if (attribution == null || string.IsNullOrEmpty(attribution.Network))
                return;
            Debug.Log($"Adjust attribution received. Network: {attribution.Network}, Campaign: {attribution.Campaign}");
            InstallNetworkDimension.Report(attribution.Network);
        }

        public void SetUserId(string userId)
        {
        }

        public void SetUserProperty(string propertyName, string value)
        {
        }

        public void SendCustomEvent(string eventToken, Dictionary<string, object> parameters = null)
        {
            AdjustEvent adjustEvent = new AdjustEvent(eventToken);
            if (parameters != null)
            {
                foreach (KeyValuePair<string, object> pair in parameters)
                {
                    adjustEvent.AddCallbackParameter(pair.Key, pair.Value.ToString());
                }
            }
            Adjust.TrackEvent(adjustEvent);
        }

        public void SendError(string errorMessage, string errorType = "")
        {
        }

        public void SendLevelStartEvent(int level, string levelName = "")
        {
            AdjustLevelBasedEventToken token = _config.AdjustLevelStartEventTokens.FirstOrDefault(token => token.Level == level);
            if (token == null)
                return;
            AdjustEvent adjustEvent = new AdjustEvent(token.EventToken);
            Adjust.TrackEvent(adjustEvent);
        }

        public void SendLevelCompleteEvent(int level, string levelName = "", float completionTime = 0)
        {
            AdjustLevelBasedEventToken token = _config.AdjustLevelCompleteEventTokens.FirstOrDefault(token => token.Level == level);
            if (token == null)
                return;
            AdjustEvent adjustEvent = new AdjustEvent(token.EventToken);
            Adjust.TrackEvent(adjustEvent);
        }

        public void SendLevelFailEvent(int level, FailType failType, string levelName = "", string failReason = "", float levelDuration = 0f)
        {
            AdjustLevelBasedEventToken token = _config.AdjustLevelFailEventTokens.FirstOrDefault(token => token.Level == level);
            if (token == null)
                return;
            AdjustEvent adjustEvent = new AdjustEvent(token.EventToken);
            Adjust.TrackEvent(adjustEvent);
        }

        public void SendPurchaseEvent(string itemId, string currency, float amount, string transactionId = "")
        {
            SendVerifiedPurchase(itemId, currency, amount, transactionId, "");
        }

        /// <summary>
        /// Reports IAP revenue to Adjust with store-side purchase verification when possible
        /// (Android needs the purchase token, iOS the transaction id). The transaction id doubles as
        /// the deduplication id so retries can never double-count revenue. Requires
        /// AnalyticsConfig.AdjustPurchaseEventToken from the Adjust dashboard.
        /// </summary>
        public void SendVerifiedPurchase(string productId, string currency, double amount, string transactionId, string purchaseToken)
        {
            string eventToken = _config.AdjustPurchaseEventToken;
            if (string.IsNullOrEmpty(eventToken))
            {
                Debug.LogWarning("[Adjust] AdjustPurchaseEventToken is empty — IAP revenue not reported.");
                return;
            }

            AdjustEvent adjustEvent = new AdjustEvent(eventToken);
            adjustEvent.SetRevenue(amount, currency);
            adjustEvent.ProductId = productId;
            if (!string.IsNullOrEmpty(transactionId))
                adjustEvent.DeduplicationId = transactionId;

#if UNITY_IOS
            adjustEvent.TransactionId = transactionId;
            Adjust.VerifyAndTrackAppStorePurchase(adjustEvent, result =>
                Debug.Log($"[Adjust] Purchase verification: {result?.VerificationStatus} (code {result?.Code}) {result?.Message}"));
#elif UNITY_ANDROID
            if (!string.IsNullOrEmpty(purchaseToken))
            {
                adjustEvent.PurchaseToken = purchaseToken;
                Adjust.VerifyAndTrackPlayStorePurchase(adjustEvent, result =>
                    Debug.Log($"[Adjust] Purchase verification: {result?.VerificationStatus} (code {result?.Code}) {result?.Message}"));
            }
            else
            {
                // No purchase token (should not happen on device) — report unverified rather than losing revenue.
                Adjust.TrackEvent(adjustEvent);
            }
#else
            Adjust.TrackEvent(adjustEvent);
#endif
        }

        public void SendAdImpressionEvent(AdType adType, string adPlacement, string adNetwork = "")
        {
            AdjustEvent adjustEvent = new AdjustEvent("eventToken");
            Adjust.TrackEvent(adjustEvent);
        }

        public void SendAdClickEvent(string adPlacement, string adNetwork = "")
        {
            AdjustEvent adjustEvent = new AdjustEvent("eventToken");
            Adjust.TrackEvent(adjustEvent);
        }

        public void SendResourceEvent(ResourceFlowType flowType, string currency, float amount, string resourceType, string resourceId)
        {
        }
    }
}
#endif

[Serializable]
public class AdjustLevelBasedEventToken
{
    public int Level;
    public string EventToken;
}
