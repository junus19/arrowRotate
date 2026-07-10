#if HAMMER_ENABLED
using System;
using UnityEngine;
using System.Threading.Tasks;
using Udo.HammerSDK.Runtime._Hammer;

namespace Gamebrain.SDK.Monetization
{
    public class HammerSDKMediationModule
    {
        private bool _isInitialized;

        public bool IsInitialized => _isInitialized;
        
        public async Task Initialize()
        {
            _isInitialized  = true;
            Hammer.Instance.MEDIATION_HasRewarded();
            await Task.CompletedTask;
        }

        #region Interstitial

        private void LoadInterstitial()
        {
            Hammer.Instance.MEDIATION_HasInterstitial(
                OnInterstitialLoaded,
                OnInterstitialLoadFailed
            );
        }

        public void ShowInterstitial()
        {
            Hammer.Instance.MEDIATION_HasInterstitial(
                () =>
                {
                    Hammer.Instance.MEDIATION_ShowInterstitial(
                        OnInterstitialShowed,
                        OnInterstitialFailedToDisplay
                    );
                },
                OnInterstitialLoadFailed
            );
        }

        private void OnInterstitialLoaded()
        {
            Debug.Log("An interstitial is already loaded or the interstitial is now loaded.");
        }
        
        private void OnInterstitialLoadFailed(string error)
        {
            Debug.Log("Inter couldn't be loaded");
        }

        private void OnInterstitialFailedToDisplay(string error)
        {
            Debug.Log("Interstitial could not be shown");
        }

        private void OnInterstitialShowed()
        {
            Debug.Log("Interstitial showed");
        }

        #endregion

        #region Rewarded

        public void ShowRewardedAd(string placementName, Action onSuccess)
        {
            Hammer.Instance.MEDIATION_HasRewarded(
                () =>
                {
                    Hammer.Instance.MEDIATION_ShowRewarded(
                        placementName,
                        ()=>
                        {
                            OnRewardedAdDisplayed();
                            onSuccess();
                            Hammer.Instance.MEDIATION_HasRewarded();
                        },
                        OnRewardedAdFailedToDisplay
                    );
                },
                OnRewardedAdLoadFailed
            );
        }

        private void OnRewardedAdLoadFailed(string error)
        {
            Debug.Log("Rewarded couldn't be loaded");
        }

        private void OnRewardedAdFailedToDisplay(string error)
        {
            Debug.Log("Rewarded could not be shown");
        }

        private void OnRewardedAdDisplayed()
        {
            Debug.Log("Rewarded showed");
        }

        #endregion

        #region Banner

        private void ShowBanner()
        {
            Hammer.Instance.MEDIATION_ShowBanner();
        }

        private void HideBanner()
        {
            Hammer.Instance.MEDIATION_DestroyBanner();
        }

        #endregion
    }
}
#endif
