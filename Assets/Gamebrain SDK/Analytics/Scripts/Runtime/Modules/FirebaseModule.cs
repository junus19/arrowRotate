#if FIREBASE_ENABLED
using System;
using Firebase;
using UnityEngine;
using Firebase.Analytics;
using Firebase.Extensions;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace GameBrain.SDK
{
    public class FirebaseModule : IAnalyticModule
    {
        private bool _isInitialized;
        private FirebaseApp _firebaseApp;

        public bool IsInitialized => _isInitialized;
        public FirebaseApp FirebaseApp => _firebaseApp;

        public FirebaseModule(AnalyticsConfig config)
        {
            
        }
        
        public async Task Initialize()
        {
            try
            {
                Debug.Log("Initializing Firebase...");

                await FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread
                (task =>
                    {
                        DependencyStatus dependencyStatus = task.Result;
                        if (dependencyStatus == DependencyStatus.Available)
                        {
                            _firebaseApp = FirebaseApp.DefaultInstance;

                            // FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                            FirebaseAnalytics.SetUserProperty("device_model", SystemInfo.deviceModel);
                            FirebaseAnalytics.SetUserProperty("os_version", SystemInfo.operatingSystem);
                            FirebaseAnalytics.SetUserProperty("app_version", Application.version);
                
                            _isInitialized = true;
                            Debug.Log("Firebase initialized successfully");
                        }
                        else
                        {
                            Debug.Log($"Firebase dependency check failed: {dependencyStatus}");
                        }
                    }
                );
            }
            catch (Exception exception)
            {
                Debug.Log($"Firebase initialization exception: {exception.Message}");
            }
        }

        public void SendCustomEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!_isInitialized) return;

            try
            {
                if (parameters != null && parameters.Count > 0)
                {
                    List<Parameter> fbParameters = new List<Parameter>();
                    foreach (KeyValuePair<string, object> param in parameters)
                    {
                        fbParameters.Add(new Parameter(param.Key, param.Value.ToString()));
                    }

                    FirebaseAnalytics.LogEvent(eventName, fbParameters.ToArray());
                }
                else
                {
                    FirebaseAnalytics.LogEvent(eventName);
                }

                Debug.Log($"Event tracked: {eventName}");
            }
            catch (Exception e)
            {
                Debug.Log($"Error tracking event {eventName}: {e.Message}");
            }
        }

        public void SendLevelStartEvent(int level, string levelName = "")
        {
            if (!_isInitialized) return;

            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventLevelStart, new Parameter[]
            {
                new Parameter("level_number", level),
                new Parameter("level_name", levelName)
            });
            Debug.Log($"Level start tracked: {level} - {levelName}");
        }

        public void SendLevelCompleteEvent(int level, string levelName = "", float completionTime = 0f)
        {
            if (!_isInitialized) return;

            FirebaseAnalytics.LogEvent("level_complete", new Parameter[]
            {
                new Parameter("level_number", level),
                new Parameter("level_name", levelName),
                new Parameter("completion_time", completionTime)
            });
            Debug.Log($"Level complete tracked: {level} - {levelName}");
        }

        public void SendLevelFailEvent(int level, FailType failType, string levelName = "", string failReason = "", float levelDuration = 0f)
        {
            if (!_isInitialized) return;

            FirebaseAnalytics.LogEvent("level_fail", new Parameter[]
            {
                new Parameter("level_number", level),
                new Parameter("level_name", levelName),
                new Parameter("fail_reason", failReason),
                new Parameter("level_duration", levelDuration)
            });
            Debug.Log($"Level fail tracked: {level} - {levelName} ({levelDuration}s)");
        }

        public void SendPurchaseEvent(string itemId, string currency, float amount, string transactionId = "")
        {
            if (!_isInitialized) return;

            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventPurchase, new Parameter[]
            {
                new Parameter(FirebaseAnalytics.ParameterItemID, itemId),
                new Parameter(FirebaseAnalytics.ParameterCurrency, currency),
                new Parameter(FirebaseAnalytics.ParameterValue, amount),
                new Parameter(FirebaseAnalytics.ParameterTransactionID, transactionId)
            });
            Debug.Log($"Purchase tracked: {itemId}");
        }

        public void SendAdImpressionEvent(AdType adType, string adPlacement, string adNetwork = "")
        {
            if (!_isInitialized) return;

            FirebaseAnalytics.LogEvent("ad_impression", new Parameter[]
            {
                new Parameter("ad_type", adType.ToString()),
                new Parameter("ad_placement", adPlacement),
                new Parameter("ad_network", adNetwork)
            });
            Debug.Log($"Ad impression tracked: {adType}");
        }

        public void SendAdClickEvent(string adPlacement, string adNetwork = "")
        {
            if (!_isInitialized) return;

            FirebaseAnalytics.LogEvent("ad_click", new Parameter[]
            {
                new Parameter("ad_type", "rewarded"),
                new Parameter("ad_placement", adPlacement),
                new Parameter("ad_network", adNetwork)
            });
            Debug.Log($"Ad click tracked: {adPlacement}");
        }

        public void SendError(string errorMessage, string errorType = "")
        {
            if (!_isInitialized) return;
            FirebaseAnalytics.LogEvent("error", new Parameter[]
            {
                new Parameter("error_type", errorType),
                new Parameter("error_message", errorMessage),
            });
            Debug.Log($"Error tracked: {errorType}");
        }

        public void SendResourceEvent(ResourceFlowType flowType, string currency, float amount, string resourceType, string resourceId)
        {
            FirebaseAnalytics.LogEvent("resource", new Parameter[]
            {
                new Parameter("flow_type", flowType.ToString()),
                new Parameter("currency", currency),
                new Parameter("amount", amount),
                new Parameter("resource_type", resourceType),
                new Parameter("resource_id", resourceId),
            });
        }
        
        public void SetUserProperty(string propertyName, string value)
        {
            if (!_isInitialized) return;

            FirebaseAnalytics.SetUserProperty(propertyName, value);
            Debug.Log($"User property set: {propertyName} = {value}");
        }

        public void SetUserId(string userId)
        {
            if (!_isInitialized) return;

            FirebaseAnalytics.SetUserId(userId);

            Debug.Log($"User ID set: {userId}");
        }
    }
}
#endif
