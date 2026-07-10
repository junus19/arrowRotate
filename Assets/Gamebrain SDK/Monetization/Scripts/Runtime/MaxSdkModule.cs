#if ADJUST_ENABLED
using AdjustSdk;
#endif
#if MAX_ENABLED
using System;
using GameBrain.SDK.Monetization.Util;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace GameBrain.SDK
{
    public class MaxSdkModule
    {
        private readonly string _interstitialAdUnitId;
        private readonly string _rewardedAdUnitId;
        private readonly string _bannerAdUnitId;
        private readonly AnalyticsConfig _config;
        private bool _isBannerShowing;
        private bool _bannerRetryScheduled;
        private const float BannerRetryDelaySeconds = 30f; // low frequency on purpose, so retries never impact gameplay
        private const float InitWaitTimeoutSeconds = 30f;  // hard cap so the init poll can never spin forever
        private bool _interstitialRetryScheduled;
        private int _interstitialRetryAttempt;
        private int _rewardedRetryAttempt;
        private float _timeout = 3;
        
        private Action _rewardedOnSuccess;
        private Action _rewardedOnFailure;

        public static event Action<string, MaxSdkBase.Reward, MaxSdkBase.AdInfo> OnRewardedAdReceiveReward;

        public MaxSdkModule(AnalyticsConfig config)
        {
            _config = config;
#if UNITY_IOS
                _interstitialAdUnitId = config.IosInterstitialAdUnitId;
                _rewardedAdUnitId = config.IosRewardedAdUnitId;
                _bannerAdUnitId = config.IosBannerAdUnitId;
#elif UNITY_ANDROID
            _interstitialAdUnitId = config.AndroidInterstitialAdUnitId;
            _rewardedAdUnitId = config.AndroidRewardedAdUnitId;
            _bannerAdUnitId = config.AndroidBannerAdUnitId;
#endif
        }
        
        public async Task Initialize()
        {
            try
            {
                Debug.Log("Initializing MAX SDK...");
                
#if UNITY_IOS || UNITY_ANDROID
                MaxSdkCallbacks.OnSdkInitializedEvent += OnMAXSDKInitialized;
                MaxSdk.SetUserId(SystemInfo.deviceUniqueIdentifier);
                MaxSdk.InitializeSdk();
                
                float initWaitElapsed = 0f;
                while (!MaxSdk.GetSdkConfiguration().IsSuccessfullyInitialized)
                {
                    await Task.Delay(100);
                    if (!Application.isPlaying) return; // Editor: play mode ended while waiting
                    initWaitElapsed += 100;
                    if (initWaitElapsed >= InitWaitTimeoutSeconds * 1000)
                    {
                        // Don't poll forever: ad units still get wired up via OnSdkInitializedEvent
                        // whenever the SDK eventually comes up, so it is safe to stop waiting here.
                        Debug.Log("MAX SDK init wait timed out; continuing without blocking.");
                        return;
                    }
                }

                float elapsedTime = 0f;
                while (!MaxSdk.IsRewardedAdReady(_rewardedAdUnitId))
                {
                    await Task.Delay(100);
                    if (!Application.isPlaying) return; // Editor: play mode ended while waiting
                    elapsedTime += 100;
                    if (elapsedTime >= _timeout * 1000 && !_config.InternetRequired)
                        break;
                }
#else
            Debug.Log("MAX SDK Initialized.");
#endif
            }
            catch (Exception exception)
            {
                Debug.Log($"MAX SDK initialization exception: {exception.Message}");
            }
        }

        private void OnMAXSDKInitialized(MaxSdkBase.SdkConfiguration sdkConfiguration)
        {
            Debug.Log("MAX SDK Initialized.");
            InitializeRewardedAds();
            InitializeInterstitialAds();
            InitializeBannerAds();
        }

        #region Interstitial
        
        private void InitializeInterstitialAds()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += InterstitialFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialDismissedEvent;
            LoadInterstitial();
        }

        private void LoadInterstitial()
        {
            MaxSdk.LoadInterstitial(_interstitialAdUnitId);
        }

        public void ShowInterstitial()
        {
            if (MaxSdk.IsInterstitialReady(_interstitialAdUnitId))
                MaxSdk.ShowInterstitial(_interstitialAdUnitId);
        }

        private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Interstitial loaded.");
            _interstitialRetryAttempt = 0;
        }

        private void OnInterstitialFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            Debug.Log("Interstitial failed to load with error code: " + errorInfo.Code);
            if (!_config.InternetRequired) return;
            ScheduleInterstitialRetry();
        }

        /// <summary>
        /// Retries the interstitial load with exponential backoff (2s → 4s, capped). Single-flight:
        /// overlapping failures never stack extra retries. The attempt counter resets on a successful load.
        /// </summary>
        private async void ScheduleInterstitialRetry()
        {
            if (_interstitialRetryScheduled) return;
            _interstitialRetryScheduled = true;

            _interstitialRetryAttempt++;
            double retryDelay = Math.Pow(2, Math.Min(2, _interstitialRetryAttempt));
            await Task.Delay((int)(retryDelay * 1000));
            _interstitialRetryScheduled = false;

            if (!Application.isPlaying) return; // Editor: play mode ended while the delay was pending

            LoadInterstitial();
        }

        private void InterstitialFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Interstitial failed to display with error code: " + errorInfo.Code);
            LoadInterstitial();
        }

        private void OnInterstitialDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Interstitial dismissed");
            LoadInterstitial();
        }
        
        #endregion

        #region Rewarded
        
        private void InitializeRewardedAds()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdDismissedEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
            LoadRewardedAd(); // Load the first RewardedAd
        }

        private void LoadRewardedAd()
        {
            MaxSdk.LoadRewardedAd(_rewardedAdUnitId);
        }

        public void ShowRewardedAd(string placement, Action onSuccess = null, Action onFailure = null)
        {
            _rewardedOnSuccess = onSuccess;
            _rewardedOnFailure = onFailure;
            
            if (Application.isEditor)
            {
                onSuccess?.Invoke();
                _rewardedOnSuccess = null;
                _rewardedOnFailure = null;
                return;
            }
            
            if (MaxSdk.IsRewardedAdReady(_rewardedAdUnitId))
                MaxSdk.ShowRewardedAd(_rewardedAdUnitId, placement);
            else
            {
                _rewardedOnFailure?.Invoke();
                _rewardedOnSuccess = null;
                _rewardedOnFailure = null;
            }
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Rewarded ad loaded");
            _rewardedRetryAttempt = 0;
        }

        private void OnRewardedAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            // _rewardedRetryAttempt++;
            // double retryDelay = Math.Pow(2, Math.Min(6, _rewardedRetryAttempt));
            // MethodInvoker.Current.Invoke("LoadRewardedAd", (float)retryDelay);
            _rewardedOnFailure?.Invoke();
            _rewardedOnSuccess = null;
            _rewardedOnFailure = null;
            Debug.Log("Rewarded ad failed to load with error code: " + errorInfo.Code);
        }

        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            _rewardedOnFailure?.Invoke();
            _rewardedOnSuccess = null;
            _rewardedOnFailure = null;
            // Rewarded ad failed to display. We recommend loading the next ad
            Debug.Log("Rewarded ad failed to display with error code: " + errorInfo.Code);
            // LoadRewardedAd();
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Rewarded ad displayed");
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Rewarded ad clicked");
        }

        private void OnRewardedAdDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            _rewardedOnFailure?.Invoke();
            _rewardedOnSuccess = null;
            _rewardedOnFailure = null;
            // Rewarded ad is hidden. Pre-load the next ad
            Debug.Log("Rewarded ad dismissed");
            LoadRewardedAd();
        }

        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            _rewardedOnSuccess?.Invoke();
            _rewardedOnSuccess = null;
            _rewardedOnFailure = null;
            // Rewarded ad was displayed and user should receive the reward
            Debug.Log("Rewarded ad received reward");
// #if ADJUST_ENABLED
//             Adjust.TrackAdRevenue(new AdjustAdRevenue(adInfo.NetworkName));
// #endif
            OnRewardedAdReceiveReward?.Invoke(adUnitId, reward, adInfo);
        }

        #endregion

        #region Banner

        private void InitializeBannerAds()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent += OnBannerAdFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent += OnBannerAdClickedEvent;

            MaxSdk.CreateBanner(_bannerAdUnitId, new MaxSdkBase.AdViewConfiguration(MaxSdkBase.AdViewPosition.BottomCenter));
            MaxSdk.SetBannerBackgroundColor(_bannerAdUnitId, Color.black);

            // Hide the banner immediately if No Ads gets purchased mid-session.
            Monetization.NoAdsState.OnChanged += OnNoAdsStateChanged;

            if (_config.BannerEnabled && !Monetization.NoAdsState.Removed)
            {
                MaxSdk.ShowBanner(_bannerAdUnitId);
                _isBannerShowing = true;
                Monetization.BannerState.Set(true, GetBannerHeightPx());
            }
            else
            {
                MaxSdk.HideBanner(_bannerAdUnitId);
                Monetization.BannerState.Set(false, 0f);
            }
        }

        private void OnNoAdsStateChanged()
        {
            if (!Monetization.NoAdsState.Removed) return;
            MaxSdk.HideBanner(_bannerAdUnitId);
            _isBannerShowing = false;
            Monetization.BannerState.Set(false, 0f); // nav bar drops its banner offset on this
        }

        private void ToggleBannerVisibility()
        {
            if (!_isBannerShowing)
                MaxSdk.ShowBanner(_bannerAdUnitId);
            else
                MaxSdk.HideBanner(_bannerAdUnitId);

            _isBannerShowing = !_isBannerShowing;
            Monetization.BannerState.Set(_isBannerShowing, _isBannerShowing ? GetBannerHeightPx() : 0f);
        }

        // Banner height in screen pixels: MAX reports dp, density converts to px.
        // Both MAX utils can return bogus values, so every input is sanitized — a visible banner must
        // never be reported with a non-positive height (UI would skip its offset entirely).
        private static float GetBannerHeightPx()
        {
#if UNITY_EDITOR
            // MAX's editor stub banner (BannerBottom.prefab) is a fixed 168px strip; the dp math
            // (50dp × editor density 1) undershoots it badly and left UI under the fake banner.
            return 168f;
#else
            float heightDp = MaxSdkUtils.GetAdaptiveBannerHeight();
            if (heightDp <= 0f) heightDp = 50f; // MAX returns -1 when adaptive height is unavailable; standard banner is 50dp

            float density = MaxSdkUtils.GetScreenDensity();
            if (density <= 0f) density = Screen.dpi > 0f ? Screen.dpi / 160f : 2f;

            return heightDp * density;
#endif
        }

        private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Banner ad loaded");
        }

        private void OnBannerAdFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            Debug.Log("Banner ad failed to load with error code: " + errorInfo.Code);
            ScheduleBannerRetry();
        }

        /// <summary>
        /// Retries the banner load after a fixed 30s delay. Single-flight: overlapping failures
        /// never stack extra retries, so at most one request every 30 seconds.
        /// </summary>
        private async void ScheduleBannerRetry()
        {
            if (!_config.BannerEnabled) return; // banner is disabled entirely, no point retrying
            if (Monetization.NoAdsState.Removed) return; // player paid to remove banners
            if (_bannerRetryScheduled) return;
            _bannerRetryScheduled = true;

            await Task.Delay((int)(BannerRetryDelaySeconds * 1000));
            _bannerRetryScheduled = false;

            if (!Application.isPlaying) return; // Editor: play mode ended while the delay was pending

            Debug.Log("Retrying banner ad load...");
            MaxSdk.LoadBanner(_bannerAdUnitId);
        }

        private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            Debug.Log("Banner ad clicked");
        }
        
        #endregion
    }
}
#endif
