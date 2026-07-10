using System;
using UnityEngine;
using System.Collections;
#if GAMEANALYTICS_ENABLED
using GameAnalyticsSDK;
#endif

namespace GameBrain.SDK
{
    public class RemoteConfigService
    {
        private readonly AnalyticsConfig _config;
        private bool _isInitialized = false;

        public RemoteConfigService(AnalyticsConfig config)
        {
            _config = config;
        }
        
        public IEnumerator Initialize()
        {
#if GAMEANALYTICS_ENABLED
            if (_config.GameAnalyticsEnabled)
            {
                GameAnalytics.OnRemoteConfigsUpdatedEvent += OnRemoteConfigUpdated;
                GameAnalytics.IsRemoteConfigsReady();
                while (!GameAnalytics.IsRemoteConfigsReady())
                {
                    yield return null;
                }

                OnRemoteConfigIsReady();
            }
            _isInitialized = true;
#endif
            yield break;
        }
        
        private void OnRemoteConfigIsReady()
        {
#if GAMEANALYTICS_ENABLED
            _config.AdsEnabled = string.Equals(GameAnalytics.GetRemoteConfigsValueAsString("ads_enabled", "true"), "true", StringComparison.OrdinalIgnoreCase);
            _config.InterstitialEnabled = string.Equals(GameAnalytics.GetRemoteConfigsValueAsString("inter_Enabled", "false"), "true", StringComparison.OrdinalIgnoreCase);
            Debug.Log($"Remote Config - Level Bucket Index: {int.Parse(GameAnalytics.GetRemoteConfigsValueAsString("levels_index"))}");
#endif
        }
        
        private void OnRemoteConfigUpdated()
        {
            
        }
    }
}
