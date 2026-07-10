using UnityEngine;

#if GAMEANALYTICS_ENABLED
using GameAnalyticsSDK;
#endif

namespace GameBrain.SDK
{
    // Forwards the Adjust install network to GameAnalytics as custom dimension 01.
    // The normalized values returned by Normalize() must also be whitelisted in the
    // GameAnalytics Settings asset (Custom Dimensions 01), otherwise the SDK drops them.
    public static class InstallNetworkDimension
    {
        private const string PrefsKey = "Install_Network";
        private static string _lastApplied;

        // Call before GameAnalytics.Initialize so the session start is tagged.
        public static void ApplyCached()
        {
            string network = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (!string.IsNullOrEmpty(network))
                SetDimension(network);
        }

        public static void Report(string rawNetwork)
        {
            string network = Normalize(rawNetwork);
            if (string.IsNullOrEmpty(network))
                return;
            if (PlayerPrefs.GetString(PrefsKey, string.Empty) != network)
            {
                PlayerPrefs.SetString(PrefsKey, network);
                PlayerPrefs.Save();
            }
            SetDimension(network);
        }

        private static void SetDimension(string network)
        {
            if (network == _lastApplied)
                return;
            _lastApplied = network;
#if GAMEANALYTICS_ENABLED
            GameAnalytics.SetCustomDimension01(network);
#endif
        }

        private static string Normalize(string rawNetwork)
        {
            if (string.IsNullOrEmpty(rawNetwork))
                return null;
            string network = rawNetwork.ToLowerInvariant();
            if (network.Contains("axon") || network.Contains("applovin"))
                return "AppLovin";
            if (network.Contains("facebook") || network.Contains("meta"))
                return "Facebook";
            if (network.Contains("instagram"))
                return "Instagram";
            if (network.Contains("organic"))
                return "Organic";
            if (network.Contains("unattr"))
                return "Unattributed";
            return "Other";
        }
    }
}
