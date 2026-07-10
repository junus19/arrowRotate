using System;
using UnityEngine;

namespace GameBrain.SDK.Monetization
{
    /// <summary>
    /// Persistent "player bought No Ads" flag. PlayerPrefs is only a local cache — the authoritative
    /// ownership record is the platform store: on Google owned products auto-restore at IAP init, on iOS
    /// the Restore button re-grants, and both paths call <see cref="SetRemoved"/> again (idempotent).
    /// Written by the store's NoAdsRewardGranter; ad code and UI only read + subscribe.
    /// No Ads removes BANNER + INTERSTITIAL only — rewarded ads are user-initiated and stay.
    /// </summary>
    public static class NoAdsState
    {
        private const string Key = "no_ads_purchased";

        public static event Action OnChanged;

        public static bool Removed => PlayerPrefs.GetInt(Key, 0) == 1;

        public static void SetRemoved()
        {
            if (Removed) return;
            PlayerPrefs.SetInt(Key, 1);
            PlayerPrefs.Save();
            Debug.Log("[NoAdsState] No Ads purchased — banner + interstitial disabled.");
            OnChanged?.Invoke();
        }

        /// <summary>Testing only: clears the local flag (a store restore will set it back).</summary>
        public static void ResetForTesting()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
            OnChanged?.Invoke();
        }
    }
}
