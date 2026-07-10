using System;
using UnityEngine;
using System.Threading.Tasks;

namespace GameBrain.SDK.Monetization
{
    public class AdService
    {
        private readonly AnalyticsConfig _config; 

#if MAX_ENABLED
        private MaxSdkModule _maxSdkModule;
#endif
#if HAMMER_ENABLED
        private HammerSDKMediationModule _hammerMediationModule;
#endif
        private bool _adsEnabled;
        private bool _isInitialized;
        private float _lastInterstitialShowTime;

        public bool IsInitialized => _isInitialized;
        public float LastInterstitialShowTime => _lastInterstitialShowTime;

        public event Action OnInitialize;

        public AdService(AnalyticsConfig config)
        {
            _config = config;
        }
        
        public async Task Initialize()
        {
            try
            {
                Debug.Log("Starting ad services initialization...");
                _lastInterstitialShowTime = 0f;
#if MAX_ENABLED
                _maxSdkModule = new MaxSdkModule(_config);
                Task maxSDKTask = _maxSdkModule.Initialize();
                await Task.WhenAll(maxSDKTask);
#elif HAMMER_ENABLED
                _hammerMediationModule = new HammerSDKMediationModule();
                Task hammerSDKTask = _hammerMediationModule?.Initialize();
                await Task.WhenAll(hammerSDKTask);
#endif 
                _isInitialized = true;
                Debug.Log("Ad Services initialized successfully.");
                OnInitialize?.Invoke();
                await Task.CompletedTask;
            }
            catch (Exception exception)
            {
                Debug.Log($"Ad Services initialization exception: {exception.Message}");
            }
        }

        public void ShowInterstitialAd()
        {
            if (NoAdsState.Removed) return; // No Ads purchased — interstitials are gone for good
            if (!_config.AdsEnabled || !_config.InterstitialEnabled)
                return;
#if MAX_ENABLED
            _maxSdkModule.ShowInterstitial();
#elif HAMMER_ENABLED
            _hammerMediationModule?.ShowInterstitial();
#endif
            _lastInterstitialShowTime = Time.time;
        }

        public void ShowRewardedAd(string placement, Action onSuccess = null, Action onFailure = null)
        {
            if (Application.isEditor)
            {
                onSuccess?.Invoke();
                return;
            }

            if (!_config.AdsEnabled || !_config.RewardedEnabled)
            {
                onFailure?.Invoke();
                return;
            }
#if MAX_ENABLED
            _maxSdkModule.ShowRewardedAd(placement, onSuccess, onFailure);
#elif HAMMER_ENABLED
            _hammerMediationModule.ShowRewardedAd(placement, onSuccess);
#endif
        }
    }
}
