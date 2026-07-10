using System;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.SDK
{
    [CreateAssetMenu(fileName = "AnalyticsConfig", menuName = "Analytics/Configuration")]
    public class AnalyticsConfig : ScriptableObject
    {
        [Header("Facebook SDK")]
        public bool FacebookEnabled;
        public string FacebookAppName = string.Empty;
        public string FacebookAppId = string.Empty;
        public string FacebookClientToken = string.Empty;

        [Header("Applovin MAX SDK")]
        public bool ApplovinEnabled;
        public string ApplovinSdkKey = "5G2ZhfKdfVwh7-HzvMz-LzwDwddWi_PpFRMy5hamBVSITkGN4DLQBGe6vGdcF1PVTnRZT1JfWmz-UDLZQh-czd";
        public bool AdReviewEnabled = true;
        public bool PrivacyFlowEnabled = true;
        public bool PrivacyFlowEnabledInGDPR = false;
        public string PrivacyTermsURL = "https://gamebra.in/hyper/policy/";
        public string AndroidInterstitialAdUnitId = string.Empty;
        public string AndroidRewardedAdUnitId = string.Empty;
        public string AndroidBannerAdUnitId = string.Empty;
        public string IosInterstitialAdUnitId = string.Empty;
        public string IosRewardedAdUnitId = string.Empty;
        public string IosBannerAdUnitId = string.Empty;

        [Header("GameAnalytics SDK")]
        public bool GameAnalyticsEnabled;
        public string GameAnalyticsAndroidGameKey = string.Empty;
        public string GameAnalyticsAndroidSecretKey = string.Empty;
        public string GameAnalyticsIosGameKey = string.Empty;
        public string GameAnalyticsIosSecretKey = string.Empty;
        public bool GameAnalyticsBuild;
        public string GameAnalyticsBuildVersion = "0.1.0";
        public string GameAnalyticsAndroidBuildVersion = "0.1.0";

        [Header("Adjust SDK")]
        public bool AdjustEnabled;
        public string AdjustAppToken = string.Empty;
        public AdjustEnvironment AdjustEnvironment = AdjustEnvironment.Production;
        public string AdjustFirstLaunchEventToken = string.Empty;
        public string AdjustTutorialStartEventToken = string.Empty;
        public string AdjustTutorialCompleteEventToken = string.Empty;
        [Tooltip("Adjust dashboard event token for IAP revenue (verified purchase). Empty = IAP revenue is not reported.")]
        public string AdjustPurchaseEventToken = string.Empty;
        public List<AdjustLevelBasedEventToken> AdjustLevelStartEventTokens = new List<AdjustLevelBasedEventToken>();
        public List<AdjustLevelBasedEventToken> AdjustLevelCompleteEventTokens = new List<AdjustLevelBasedEventToken>();
        public List<AdjustLevelBasedEventToken> AdjustLevelFailEventTokens = new List<AdjustLevelBasedEventToken>();

        [Header("Firebase SDK")]
        public bool FirebaseEnabled;
        public string FirebaseAndroidConfigPath;
        public string FirebaseIOSConfigPath;
        
        [Header("Hammer SDK")]
        public bool HammerEnabled;

        [Header("General")]
        public bool InternetRequired;
        public bool EnableInEditor;
        public bool VerboseLogging;
        public bool AutoSave;
        
        [Header("Ad Services")]
        public bool AdsEnabled = true;
        public bool InterstitialEnabled = false;
        public bool RewardedEnabled = true;
        public bool BannerEnabled = false;
        public List<AdPresetCondition> GlobalAdConditions;
        public List<AdPreset> AdPresets;
        
        public bool GlobalAdConditionsAreMet(int sessionId, int level, float amountOfTimeElapsedFromLastAd)
        {
            foreach (AdPresetCondition condition in GlobalAdConditions)
            {
                switch(condition.ConditionType)
                {
                    case ConditionType.MinimumLevel:
                        if (level < condition.Value)
                            return false;
                        break;
                    case ConditionType.Time:
                        if (amountOfTimeElapsedFromLastAd < condition.Value)
                            return false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return true;
        }

        public bool HasGlobalAdConditions() => GlobalAdConditions != null && GlobalAdConditions.Count > 0;

        public bool HasAdPreset() => AdPresets != null && AdPresets.Count > 0;

        public bool HasLevelBasedAdPreset() => AdPresets != null && AdPresets.Count > 0 && AdPresets.Any(preset => preset.Type == AdFrequency.LevelBased);

        public bool HasTimeBasedAdPreset() => AdPresets != null && AdPresets.Count > 0 && AdPresets.Any(preset => preset.Type == AdFrequency.TimeBased);

        public AdPreset GetLevelBasedAdPreset(int session)
            => AdPresets
                .Where(preset => preset.Type == AdFrequency.LevelBased)
                .OrderBy(preset => preset.MinimumSessionNumber)
                .Last(preset => preset.MinimumSessionNumber <= session);

        public AdPreset GetTimeBasedAdPreset(int session)
            => AdPresets
                .Where(preset => preset.Type == AdFrequency.TimeBased)
                .OrderBy(preset => preset.MinimumSessionNumber)
                .Last(preset => preset.MinimumSessionNumber <= session);
    }
    
    public enum AdjustEnvironment
    {
        Sandbox,
        Production
    }
}
