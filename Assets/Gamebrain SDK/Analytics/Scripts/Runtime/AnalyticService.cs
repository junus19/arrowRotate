using System;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GameBrain.SDK
{
    public class AnalyticsService
    {
        private readonly AnalyticsConfig _config;
        private readonly IAnalyticModule _gameAnalyticsModule;
        private readonly IAnalyticModule _firebaseModule;
        private readonly IAnalyticModule _adjustModule;
        private readonly IAnalyticModule _hammerModule;
        private readonly FacebookModule _facebookModule;
        private bool _isInitialized;
        private int _sessionCount;
        private InstallDateTracker _installDateTracker;

        public AnalyticsConfig Config => _config;
        public bool IsInitialized => _isInitialized;
        public int SessionCount => _sessionCount;

        public event Action OnInitialize;

        public AnalyticsService
        (
            AnalyticsConfig config,
            IAnalyticModule gameAnalyticsModule,
            IAnalyticModule firebaseModule,
            IAnalyticModule adjustModule,
            IAnalyticModule hammerModule,
            FacebookModule facebookModule
        )
        {
            _config = config;
            _gameAnalyticsModule = gameAnalyticsModule;
            _firebaseModule = firebaseModule;
            _adjustModule = adjustModule;
            _hammerModule = hammerModule;
            _facebookModule = facebookModule;
        }

        public async Task Initialize()
        {
            try
            {
                Debug.Log("Starting analytics services initialization...");

                _installDateTracker = new InstallDateTracker();
                FetchSessionCount();

                if (_facebookModule != null)
                {
                    Task facebookTask = _facebookModule.Initialize();
                    await facebookTask;
                    while (!_facebookModule.IsInitialized)
                    {
                        await Task.Delay(100);
                    }
                }

                if (_firebaseModule  != null && _config.FirebaseEnabled)
                {
                    Task firebaseTask = _firebaseModule.Initialize();
                    await firebaseTask;
                }

                if (_gameAnalyticsModule != null && _config.GameAnalyticsEnabled)
                {
                    InstallNetworkDimension.ApplyCached();
                    Task gameAnalyticsTask = _gameAnalyticsModule.Initialize();
                    await gameAnalyticsTask;
                    if (_sessionCount == 1)
                        _gameAnalyticsModule.SendCustomEvent("First Launch");
                }
                
                if (_adjustModule != null && _config.AdjustEnabled)
                {
                    Task adjustTask = _adjustModule.Initialize();
                    await adjustTask;
                    if (_sessionCount == 1)
                        _adjustModule.SendCustomEvent(_config.AdjustFirstLaunchEventToken);
                }
                
                if (_hammerModule != null && _config.HammerEnabled)
                {
                    Task hammerTask = _hammerModule.Initialize();
                    await hammerTask;
                }

                _isInitialized = true;
                
                Debug.Log("All analytics services initialized successfully.");
                
                OnInitialize?.Invoke();
            }
            catch (Exception exception)
            {
                throw new Exception(exception.Message);
            }
        }

        private void FetchSessionCount()
        {
            _sessionCount = PlayerPrefs.GetInt("Session_Count", 0);
            _sessionCount++;
            PlayerPrefs.SetInt("Session_Count", _sessionCount);
            PlayerPrefs.Save();
        }
        
        public void SetUserId(string userId)
        {
            if (!_isInitialized) return;

            if (_config.GameAnalyticsEnabled)
                _gameAnalyticsModule.SetUserId(userId);

            if (_config.FirebaseEnabled)
                _firebaseModule.SetUserId(userId);
            
            if (_config.AdjustEnabled)
                _adjustModule.SetUserId(userId);
            
            if (_config.HammerEnabled)
                _adjustModule.SetUserId(userId);
            
            Debug.Log($"User ID Set: {userId}");
        }

        public void SendCustomEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized) return;
            
            if (_config.GameAnalyticsEnabled)
                _gameAnalyticsModule.SendCustomEvent(eventName, parameters);

            if (_config.FirebaseEnabled)
                _firebaseModule.SendCustomEvent(eventName, parameters);
            
            if (_config.AdjustEnabled)
                _adjustModule.SendCustomEvent(eventName, parameters);
            
            if (_config.HammerEnabled)
                _hammerModule.SendCustomEvent(eventName, parameters);

            Debug.Log($"Custom Event: {eventName}");
        }

        /// <summary>
        /// Reports IAP revenue. Intentionally ADJUST-ONLY (product decision): attribution/ROAS runs on
        /// Adjust, so no other backend receives business events from here. Uses the store's real price +
        /// ISO currency; transactionId deduplicates, purchaseToken enables Play Store verification.
        /// </summary>
        public void SendPurchaseEvent(string productId, string currencyIsoCode, double amount, string transactionId, string purchaseToken = "")
        {
            if (!_isInitialized) return;

#if ADJUST_ENABLED
            if (_config.AdjustEnabled && _adjustModule is AdjustModule adjustModule)
                adjustModule.SendVerifiedPurchase(productId, currencyIsoCode, amount, transactionId, purchaseToken);
#endif

            Debug.Log($"Purchase Event (Adjust-only): {productId} {amount} {currencyIsoCode} tx:{transactionId}");
        }

        public void SendLevelStartEvent(int levelIndex)
        {
            if (!_isInitialized) return;

            if (_config.GameAnalyticsEnabled)
                _gameAnalyticsModule.SendLevelStartEvent(levelIndex);
            
            if (_config.FirebaseEnabled)
                _firebaseModule.SendLevelStartEvent(levelIndex);
            
            if (_config.AdjustEnabled)
                _adjustModule.SendLevelStartEvent(levelIndex);
            
            if (_config.HammerEnabled)
                _hammerModule.SendLevelStartEvent(levelIndex);
            
            Debug.Log($"Level Start Event: {levelIndex}");
        }
        
        public void SendLevelCompleteEvent(int levelIndex, float completionTime = 0f)
        {
            if (!_isInitialized) return;

            if (_config.GameAnalyticsEnabled)
                _gameAnalyticsModule.SendLevelCompleteEvent(levelIndex,"", completionTime);
            
            if (_config.FirebaseEnabled)
                _firebaseModule.SendLevelCompleteEvent(levelIndex,"", completionTime);
            
            if (_config.AdjustEnabled)
                _adjustModule.SendLevelCompleteEvent(levelIndex,"", completionTime);
            
            if (_config.HammerEnabled)
                _hammerModule.SendLevelCompleteEvent(levelIndex,"", completionTime);
            
            Debug.Log($"Level Complete Event: {levelIndex}");
        }

        public void SendLevelFailEvent(int levelIndex, FailType failType, float levelDuration = 0f)
        {
            if (!_isInitialized) return;

            if (_config.GameAnalyticsEnabled)
                _gameAnalyticsModule.SendLevelFailEvent(levelIndex, failType, "", "", levelDuration);

            if (_config.FirebaseEnabled)
                _firebaseModule.SendLevelFailEvent(levelIndex, failType, "", "", levelDuration);

            if (_config.AdjustEnabled)
                _adjustModule.SendLevelFailEvent(levelIndex, failType, "", "", levelDuration);

            if (_config.HammerEnabled)
                _hammerModule.SendLevelFailEvent(levelIndex, failType, "", "", levelDuration);

            Debug.Log($"Level Fail Event: {levelIndex}-{failType} ({levelDuration:0.0}s)");
        }
        
        public void SendError(string errorMessage, string errorType = "")
        {
            if (!_isInitialized) return;

            if (_config.GameAnalyticsEnabled)
                _gameAnalyticsModule.SendError(errorMessage, errorType);
            
            if (_config.FirebaseEnabled)
                _firebaseModule.SendError(errorMessage, errorType);

            if (_config.AdjustEnabled)
                _adjustModule.SendError(errorMessage, errorType);
            
            if (_config.HammerEnabled)
                _hammerModule.SendError(errorMessage, errorType);
            
            Debug.Log($"Error Tracked: {errorType} - {errorMessage}");
        }
        
        public void SendAdImpressionEvent(AdType adType, string adPlacement, string adNetwork = "")
        {
            if (!_isInitialized) return;
            
            if (_config.GameAnalyticsEnabled)
                _gameAnalyticsModule.SendAdImpressionEvent(adType, adPlacement, adNetwork);

            if (_config.FirebaseEnabled)
                _firebaseModule.SendAdImpressionEvent(adType, adPlacement, adNetwork);
            
            if (_config.AdjustEnabled)
                _adjustModule.SendAdImpressionEvent(adType, adPlacement, adNetwork);
            
            if (_config.HammerEnabled)
                _hammerModule.SendAdImpressionEvent(adType, adPlacement, adNetwork);
            
            Debug.Log($"Ad Impression Event: {adPlacement}");
        }
        
        public void SendAdClickEvent(string adPlacement, string adNetwork = "")
        {
            if (!_isInitialized) return;
            
            if (_config.GameAnalyticsEnabled)
                _gameAnalyticsModule.SendAdClickEvent(adPlacement, adNetwork);

            if (_config.FirebaseEnabled)
                _firebaseModule.SendAdClickEvent(adPlacement, adNetwork);
            
            if (_config.AdjustEnabled)
                _adjustModule.SendAdClickEvent(adPlacement, adNetwork);
            
            if (_config.HammerEnabled)
                _hammerModule.SendAdClickEvent(adPlacement, adNetwork);
            
            Debug.Log($"Ad Click Event: {adPlacement}");
        }
        
        public void SendResourceEvent(ResourceFlowType flowType, string currency, float amount, string resourceType, string resourceId)
        {
            if (!_isInitialized) return;
            
            if (_config.GameAnalyticsEnabled)
                _gameAnalyticsModule.SendResourceEvent(flowType, currency, amount, resourceType, resourceId);

            if (_config.FirebaseEnabled)
                _firebaseModule.SendResourceEvent(flowType, currency, amount, resourceType, resourceId);

            if (_config.AdjustEnabled)
                _adjustModule.SendResourceEvent(flowType, currency, amount, resourceType, resourceId);

            if (_config.HammerEnabled)
                _hammerModule.SendResourceEvent(flowType, currency, amount, resourceType, resourceId);
            
            Debug.Log($"Resource Event: {resourceType}");
        }
    }

    public enum FailType
    {
        Soft,
        Hard
    }
}
