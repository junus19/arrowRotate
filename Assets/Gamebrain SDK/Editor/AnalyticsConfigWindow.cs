using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using Object = UnityEngine.Object;

#if FACEBOOK_ENABLED
using Facebook.Unity;
using Facebook.Unity.Editor;
using Facebook.Unity.Settings;
#endif

#if GAMEANALYTICS_ENABLED
using GameAnalyticsSDK;
using GameAnalyticsSDK.Setup;
#endif

#if MAX_ENABLED
using AppLovinMax.Scripts.IntegrationManager.Editor;
#endif

#if ADJUST_ENABLED
using AdjustSdk;
#endif

#if FIREBASE_ENABLED
using Firebase.Analytics;
#endif

namespace GameBrain.SDK.Editor
{
    public class AnalyticsConfigWindow : EditorWindow
    {
        private AnalyticsConfig _configData;
        private SerializedObject _serializedConfigData;
        private Vector2 _scrollPosition;
        private int _activeTabIndex;
        private string _statusMessage = string.Empty;
        private MessageType _statusType = MessageType.None;
        private double _statusHideDuration;
        private int _autoSaveFrequency = 30;
        private double _lastSaveTime;

        private bool _gameAnalyticsAndroidCredentialsFoldout = true;
        private bool _gameAnalyticsIosCredentialsFoldout = true;
        private bool _adjustFoldout = true;
        private bool _firebaseFoldout = true;

        private bool _firebaseAndroidCredentialsFoldout = true;
        private bool _firebaseIosCredentialsFoldout = true;
        
        private bool _isGameAnalyticsInstalled;
        private bool _isFirebaseInstalled;
        private bool _isFacebookInstalled;
        private bool _isAdjustInstalled;
        private bool _isMaxInstalled;
        private bool _isHammerInstalled;

        public static readonly string[] AllAnalyticsSymbols = new string[]
        {
            AnalyticsDefineSymbols.GameAnalyticsSymbol,
            AnalyticsDefineSymbols.FirebaseSymbol,
            AnalyticsDefineSymbols.AdjustSymbol,
            AnalyticsDefineSymbols.FacebookSymbol,
            AnalyticsDefineSymbols.ApplovinSymbol,
            AnalyticsDefineSymbols.HammerSymbol
        };

        public static class AnalyticsDefineSymbols
        {
            public const string GameAnalyticsSymbol = "GAMEANALYTICS_ENABLED";
            public const string FirebaseSymbol = "FIREBASE_ENABLED";
            public const string AdjustSymbol = "ADJUST_ENABLED";
            public const string FacebookSymbol = "FACEBOOK_ENABLED";
            public const string ApplovinSymbol = "MAX_ENABLED";
            public const string HammerSymbol = "HAMMER_ENABLED";
        }

        private static class AnalyticsEditorWindowStyles
        {
            public static GUIStyle Header;
            public static GUIStyle SubHeader;
            public static GUIStyle SdkCard;
            public static GUIStyle SdkCardEnabled;
            public static GUIStyle StatusBadgeEnabled;
            public static GUIStyle StatusBadgeDisabled;
            public static GUIStyle SectionTitle;
            public static GUIStyle Divider;
            public static bool Initialized;

            public static void Init()
            {
                if (Initialized) return;

                Header = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleLeft,
                    normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
                };

                SubHeader = new GUIStyle(EditorStyles.label)
                {
                    fontSize = 11,
                    normal = { textColor = new Color(0.55f, 0.55f, 0.55f) },
                };

                SdkCard = new GUIStyle("HelpBox")
                {
                    padding = new RectOffset(12, 12, 8, 8),
                    margin = new RectOffset(0, 0, 4, 4)
                };

                SdkCardEnabled = new GUIStyle(SdkCard);

                StatusBadgeEnabled = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.4f, 0.9f, 0.5f) }
                };

                StatusBadgeDisabled = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                };

                SectionTitle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 12,
                    normal = { textColor = new Color(0.85f, 0.85f, 0.85f) }
                };

                Divider = new GUIStyle()
                {
                    margin = new RectOffset(0, 0, 6, 6),
                    fixedHeight = 1
                };

                Initialized = true;
            }
        }

        [Serializable]
        private class SdkInfo
        {
            public string Name;
            public string Version;
            public string Description;
            public Color AccentColor;
            public bool IsInstalled;
        }

        private static readonly SdkInfo[] _sdkInfos = new[]
        {
            new SdkInfo { Name = "Facebook", Version = "x.x", Description = "App Events, Attribution & Audience Network", AccentColor = new Color(0.26f, 0.40f, 0.70f) },
            new SdkInfo { Name = "Applovin MAX", Version = "x.x", Description = "Mediation, Ads & Revenue Tracking", AccentColor = new Color(0.90f, 0.30f, 0.20f) },
            new SdkInfo { Name = "GameAnalytics", Version = "x.x", Description = "Player Behavior & Progression Analytics", AccentColor = new Color(0.20f, 0.72f, 0.45f) },
            new SdkInfo { Name = "Adjust", Version = "x.x", Description = "Mobile Attribution & Marketing Analytics", AccentColor = new Color(0.00f, 0.60f, 0.78f) },
            new SdkInfo { Name = "Firebase", Version = "x.x", Description = "Analytics, Crashlytics, Remote Config & FCM", AccentColor = new Color(1.00f, 0.65f, 0.00f) },
        };

        private static readonly string[] _tabLabels = { "Overview", "Ad Presets", "Facebook", "AppLovin", "GameAnalytics", "Adjust", "Firebase", "Settings" };

        [MenuItem("Tools/Gamebra.in SDK Configurator")]
        public static void OpenWindow()
        {
            AnalyticsConfigWindow window = GetWindow<AnalyticsConfigWindow>("Gamebra.in Sdk Configurator");
            window.minSize = new Vector2(480, 640);
            window.Show();
        }

        private void OnEnable()
        {
            _lastSaveTime = 0f;
            _lastFirebaseConfigCheckTime = 0f;
            LoadOrCreateConfig();
            CheckInstalledPackages();
            AnalyticsEditorWindowStyles.Initialized = false;
        }

        private void OnDisable()
        {
            if (_configData == null)
                return;
            if (_configData.AutoSave)
                SaveConfig();
        }

        private void OnLostFocus()
        {
            if (_configData == null)
                return;
            if (_configData.AutoSave)
                SaveConfig();
        }

        private void OnGUI()
        {
            AnalyticsEditorWindowStyles.Init();

            if (_configData == null)
            {
                DrawNoConfigState();
                return;
            }

            if (_serializedConfigData == null) _serializedConfigData = new SerializedObject(_configData);

            _serializedConfigData.Update();

            DrawTabBar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            EditorGUILayout.Space(6);


            GUILayout.BeginVertical(new GUIStyle() { padding = new RectOffset(10, 10, 10, 10) });
            switch (_activeTabIndex)
            {
                case 0: DrawOverviewTab(); break;
                case 1: DrawAdSettingsTab(); break;
                case 2: DrawFacebookTab(); break;
                case 3: DrawApplovinTab(); break;
                case 4: DrawGameAnalyticsTab(); break;
                case 5: DrawAdjustTab(); break;
                case 6: DrawFirebaseTab(); break;
                case 7: DrawSettingsTab(); break;
            }

            GUILayout.EndVertical();
            EditorGUILayout.Space(12);
            EditorGUILayout.EndScrollView();

            DrawFooter();

            if (_serializedConfigData.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(_configData);
            }

            if (!string.IsNullOrEmpty(_statusMessage) && EditorApplication.timeSinceStartup > _statusHideDuration)
                _statusMessage = string.Empty;

            if (_configData.AutoSave && Time.time - _lastSaveTime >= _autoSaveFrequency)
                SaveConfig();
        }

        private void DrawTabBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            for (int i = 0; i < _tabLabels.Length; i++)
            {
                bool selected = (_activeTabIndex == i);
                GUIStyle style = EditorStyles.toolbarButton;
                if (GUILayout.Toggle(selected, _tabLabels[i], style))
                    _activeTabIndex = i;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawOverviewTab()
        {
            GUILayout.Label("Ad Service Overview", AnalyticsEditorWindowStyles.SectionTitle);
            DrawAdServiceOverviewCard();
            EditorGUILayout.Space(8);

            GUILayout.Label("SDK Status Overview", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUILayout.Space(4);

            DrawSdkOverviewCard(0, _configData.FacebookEnabled, "App ID", !string.IsNullOrEmpty(_configData.FacebookAppId), _isFacebookInstalled);
            DrawSdkOverviewCard(1, _configData.ApplovinEnabled, "SDK Key", !string.IsNullOrEmpty(_configData.ApplovinSdkKey), _isMaxInstalled);
            DrawSdkOverviewCard(2, _configData.GameAnalyticsEnabled, "Game Key", !string.IsNullOrEmpty(_configData.GameAnalyticsAndroidGameKey), _isGameAnalyticsInstalled);
            DrawSdkOverviewCard(3, _configData.AdjustEnabled, "App Token", !string.IsNullOrEmpty(_configData.AdjustAppToken), _isAdjustInstalled);
            DrawSdkOverviewCard(4, _configData.FirebaseEnabled, string.Empty, true, _isFirebaseInstalled);

            EditorGUILayout.Space(8);
            DrawHorizontalLine();
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Enable All SDKs", GUILayout.Height(20)))
            {
                _configData.FacebookEnabled = _configData.ApplovinEnabled = _configData.GameAnalyticsEnabled = _configData.AdjustEnabled = _configData.FirebaseEnabled = true;
                ShowStatus("All SDKs enabled.", MessageType.Info);
            }

            if (GUILayout.Button("Disable All SDKs", GUILayout.Height(20)))
            {
                _configData.FacebookEnabled = _configData.ApplovinEnabled = _configData.GameAnalyticsEnabled = _configData.AdjustEnabled = _configData.FirebaseEnabled = false;
                ShowStatus("All SDKs disabled.", MessageType.Warning);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAdServiceOverviewCard()
        {
            EditorGUILayout.BeginHorizontal(AnalyticsEditorWindowStyles.SdkCard);

            Rect accentRect = GUILayoutUtility.GetRect(4, 48, GUILayout.Width(4));
            EditorGUI.DrawRect(accentRect, _configData.AdsEnabled ? Color.burlywood : new Color(0.3f, 0.3f, 0.3f));

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Ad Service", AnalyticsEditorWindowStyles.SectionTitle);
            GUILayout.Label("Ad Presets, Conditions & Configuration", AnalyticsEditorWindowStyles.SubHeader);

            Color installStatusLabelColor = new Color(0.4f, 0.8f, 0.5f);
            Color prevColor = GUI.color;
            GUI.color = installStatusLabelColor;
            GUILayout.Label($"✓ Installed", EditorStyles.miniLabel);
            GUI.color = prevColor;
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical(GUILayout.Width(110));

            string statusText = _configData.AdsEnabled ? "● ENABLED" : "○ DISABLED";
            GUIStyle statusStyle = _configData.AdsEnabled ? AnalyticsEditorWindowStyles.StatusBadgeEnabled : AnalyticsEditorWindowStyles.StatusBadgeDisabled;
            GUILayout.Label(statusText, statusStyle);


            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Configure", EditorStyles.miniButton, GUILayout.Width(72), GUILayout.Height(32)))
                _activeTabIndex = _activeTabIndex + 1;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSdkOverviewCard(int sdkIndex, bool enabled, string keyLabel, bool keySet, bool isInstalled)
        {
            SdkInfo sdk = _sdkInfos[sdkIndex];

            EditorGUILayout.BeginHorizontal(AnalyticsEditorWindowStyles.SdkCard);

            Rect accentRect = GUILayoutUtility.GetRect(4, 48, GUILayout.Width(4));
            EditorGUI.DrawRect(accentRect, enabled ? sdk.AccentColor : new Color(0.3f, 0.3f, 0.3f));

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();
            GUILayout.Label(sdk.Name, AnalyticsEditorWindowStyles.SectionTitle);
            GUILayout.Label(sdk.Description, AnalyticsEditorWindowStyles.SubHeader);

            Color installStatusLabelColor = isInstalled ? new Color(0.4f, 0.8f, 0.5f) : new Color(1f, 0f, 0.2f);
            Color prevColor = GUI.color;
            GUI.color = installStatusLabelColor;
            GUILayout.Label(isInstalled ? $"✓ Installed" : $"✗ Not Found!", EditorStyles.miniLabel);
            GUI.color = prevColor;
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            EditorGUILayout.BeginVertical(GUILayout.Width(110));

            string statusText = enabled ? "● ENABLED" : "○ DISABLED";
            GUIStyle statusStyle = enabled ? AnalyticsEditorWindowStyles.StatusBadgeEnabled : AnalyticsEditorWindowStyles.StatusBadgeDisabled;
            GUILayout.Label(statusText, statusStyle);

            if (!string.IsNullOrEmpty(keyLabel))
            {
                Color keyColor = keySet ? new Color(0.4f, 0.8f, 0.5f) : new Color(1f, 0f, 0.2f);
                prevColor = GUI.color;
                GUI.color = keyColor;
                GUILayout.Label(keySet ? $"✓ {keyLabel} configured" : $"✗ {keyLabel} missing", EditorStyles.miniLabel);
                GUI.color = prevColor;
            }

            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Configure", EditorStyles.miniButton, GUILayout.Width(72), GUILayout.Height(32)))
                _activeTabIndex = sdkIndex + 2;

            EditorGUILayout.EndHorizontal();
        }

        private void DrawAdSettingsTab()
        {
            EditorGUILayout.BeginHorizontal();

            Rect iconRect = GUILayoutUtility.GetRect(6, 32, GUILayout.Width(6));
            EditorGUI.DrawRect(iconRect, _configData.AdsEnabled ? Color.burlywood : new Color(0.3f, 0.3f, 0.3f));

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();
            GUILayout.Label("Ad Service", AnalyticsEditorWindowStyles.SectionTitle);
            GUILayout.Label($"v1.0.0 — Ad Presets, Conditions & Configuration", AnalyticsEditorWindowStyles.SubHeader);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            _configData.AdsEnabled = EditorGUILayout.Toggle(_configData.AdsEnabled, GUILayout.Width(20));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            DrawHorizontalLine();
            EditorGUILayout.Space(6);

            GUILayout.Label("Interstitial", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUI.indentLevel++;
            _serializedConfigData.FindProperty("InterstitialEnabled").boolValue =
                EditorGUILayout.Toggle(new GUIContent("Interstitial Enabled"), _configData.InterstitialEnabled, GUILayout.Width(20));
            if (_configData.InterstitialEnabled)
            {
                EditorGUILayout.PropertyField(_serializedConfigData.FindProperty("GlobalAdConditions"), new GUIContent("Conditions"));
                EditorGUILayout.PropertyField(_serializedConfigData.FindProperty("AdPresets"), new GUIContent("Presets"));
            }

            EditorGUI.indentLevel--;

            GUILayout.Space(4);
            GUILayout.Label("Rewarded", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUI.indentLevel++;
            _serializedConfigData.FindProperty("RewardedEnabled").boolValue =
                EditorGUILayout.Toggle(new GUIContent("Rewarded Enabled"), _configData.RewardedEnabled, GUILayout.Width(20));
            EditorGUI.indentLevel--;
            GUILayout.Space(4);

            GUILayout.Label("Banner", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUI.indentLevel++;
            _serializedConfigData.FindProperty("BannerEnabled").boolValue =
                EditorGUILayout.Toggle(new GUIContent("Banner Enabled"), _configData.BannerEnabled, GUILayout.Width(20));
            EditorGUI.indentLevel--;
        }

        private void DrawFacebookTab()
        {
            bool oldValue = _configData.FacebookEnabled;
            DrawSdkHeader(0, ref _configData.FacebookEnabled);
            if (oldValue != _configData.FacebookEnabled)
            {
                SetDefineSymbol(AnalyticsDefineSymbols.FacebookSymbol, _configData.FacebookEnabled, EditorUserBuildSettings.selectedBuildTargetGroup);
                ApplyToAllPlatforms();
            }

            if (!_sdkInfos.First(info => info.Name == "Facebook").IsInstalled)
            {
                DrawNotFoundNotice("Facebook SDK");
                return;
            }

            if (!_configData.FacebookEnabled)
            {
                DrawDisabledNotice("Facebook SDK");
                return;
            }

            GUILayout.Label("Credentials", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUI.indentLevel++;
            _serializedConfigData.FindProperty("FacebookAppName").stringValue = DrawTextField("App Name", _configData.FacebookAppName, "From developers.facebook.com");
            _serializedConfigData.FindProperty("FacebookAppId").stringValue = DrawTextField("App ID", _configData.FacebookAppId, "From developers.facebook.com");
            _serializedConfigData.FindProperty("FacebookClientToken").stringValue =
                DrawTextField("Client Token", _configData.FacebookClientToken, "Settings → Advanced → Client Token");
            EditorGUI.indentLevel--;
        }

        private void DrawApplovinTab()
        {
            bool oldValue = _configData.ApplovinEnabled;
            DrawSdkHeader(1, ref _configData.ApplovinEnabled);
            if (oldValue != _configData.ApplovinEnabled)
            {
                SetDefineSymbol(AnalyticsDefineSymbols.ApplovinSymbol, _configData.ApplovinEnabled, EditorUserBuildSettings.selectedBuildTargetGroup);
                ApplyToAllPlatforms();
            }

            if (!_sdkInfos.First(info => info.Name == "Applovin MAX").IsInstalled)
            {
                DrawNotFoundNotice("Applovin Max SDK");
                return;
            }

            if (!_configData.ApplovinEnabled)
            {
                DrawDisabledNotice("Applovin MAX SDK");
                return;
            }

            GUILayout.Label("Credentials", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUI.indentLevel++;
            _configData.ApplovinSdkKey = DrawTextField("SDK Key", _configData.ApplovinSdkKey, "From Applovin Dashboard → Account → Keys");
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(8);
            GUILayout.Label("Privacy", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUI.indentLevel++;
            _configData.PrivacyFlowEnabled = EditorGUILayout.Toggle(new GUIContent("Privacy Flow"), _configData.PrivacyFlowEnabled, GUILayout.Width(20));
            _configData.PrivacyFlowEnabledInGDPR = EditorGUILayout.Toggle(new GUIContent("Privacy Flow In GDPR"), _configData.PrivacyFlowEnabledInGDPR, GUILayout.Width(20));
            _configData.PrivacyTermsURL = DrawTextField("Privacy Terms Url", _configData.PrivacyTermsURL, "");
            EditorGUI.indentLevel--;


            EditorGUILayout.Space(8);
            GUILayout.Label("Ad Unit IDs", AnalyticsEditorWindowStyles.SectionTitle);

            // Android
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Android", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUI.indentLevel++;
            _configData.AndroidInterstitialAdUnitId = DrawTextField("Interstitial", _configData.AndroidInterstitialAdUnitId, "From MAX → Ad Units");
            _configData.AndroidRewardedAdUnitId = DrawTextField("Rewarded Video", _configData.AndroidRewardedAdUnitId, "From MAX → Ad Units");
            _configData.AndroidBannerAdUnitId = DrawTextField("Banner", _configData.AndroidBannerAdUnitId, "From MAX → Ad Units (optional)");
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;

            // // IOS
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Ios", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUI.indentLevel++;
            _configData.IosInterstitialAdUnitId = DrawTextField("Interstitial", _configData.IosInterstitialAdUnitId, "From MAX → Ad Units");
            _configData.IosRewardedAdUnitId = DrawTextField("Rewarded Video", _configData.IosRewardedAdUnitId, "From MAX → Ad Units");
            _configData.IosBannerAdUnitId = DrawTextField("Banner", _configData.IosBannerAdUnitId, "From MAX → Ad Units (optional)");
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }

        private void DrawGameAnalyticsTab()
        {
            bool oldValue = _configData.GameAnalyticsEnabled;
            DrawSdkHeader(2, ref _configData.GameAnalyticsEnabled);
            if (oldValue != _configData.GameAnalyticsEnabled)
            {
                SetDefineSymbol(AnalyticsDefineSymbols.GameAnalyticsSymbol, _configData.GameAnalyticsEnabled, EditorUserBuildSettings.selectedBuildTargetGroup);
                ApplyToAllPlatforms();
            }

            if (!_sdkInfos.First(info => info.Name == "GameAnalytics").IsInstalled)
            {
                DrawNotFoundNotice("GameAnalytics SDK");
                return;
            }

            if (!_configData.GameAnalyticsEnabled)
            {
                DrawDisabledNotice("GameAnalytics SDK");
                return;
            }

            GUILayout.Label("Credentials", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUI.indentLevel++;

            _gameAnalyticsAndroidCredentialsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_gameAnalyticsAndroidCredentialsFoldout, "Android");
            if (_gameAnalyticsAndroidCredentialsFoldout)
            {
                EditorGUI.indentLevel++;
                _configData.GameAnalyticsAndroidGameKey = DrawTextField("Game Key", _configData.GameAnalyticsAndroidGameKey, "From GameAnalytics Dashboard → Game Keys");
                _configData.GameAnalyticsAndroidSecretKey =
                    DrawTextField("Secret Key", _configData.GameAnalyticsAndroidSecretKey, "Keep private — used for server-side validation");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();


            _gameAnalyticsIosCredentialsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_gameAnalyticsIosCredentialsFoldout, "Ios");
            if (_gameAnalyticsIosCredentialsFoldout)
            {
                EditorGUI.indentLevel++;
                _configData.GameAnalyticsIosGameKey = DrawTextField("Game Key", _configData.GameAnalyticsIosGameKey, "From GameAnalytics Dashboard → Game Keys");
                _configData.GameAnalyticsIosSecretKey = DrawTextField("Secret Key", _configData.GameAnalyticsIosSecretKey, "Keep private — used for server-side validation");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(4);
            GUILayout.Label("Build Settings", AnalyticsEditorWindowStyles.SectionTitle);
            _configData.GameAnalyticsBuild = EditorGUILayout.Toggle(new GUIContent("Custom Build Version", "Override the build version string sent to GameAnalytics"),
                _configData.GameAnalyticsBuild);
            if (_configData.GameAnalyticsBuild)
            {
                EditorGUI.indentLevel++;
                _configData.GameAnalyticsBuildVersion = DrawTextField("Build Version", _configData.GameAnalyticsBuildVersion, "e.g. 1.0.0 or 1.0.0.100");
                EditorGUI.indentLevel--;
            }
        }

        private void DrawAdjustTab()
        {
            bool oldValue = _configData.AdjustEnabled;
            DrawSdkHeader(3, ref _configData.AdjustEnabled);
            if (oldValue != _configData.AdjustEnabled)
            {
                SetDefineSymbol(AnalyticsDefineSymbols.AdjustSymbol, _configData.AdjustEnabled, EditorUserBuildSettings.selectedBuildTargetGroup);
                ApplyToAllPlatforms();
            }

            if (!_sdkInfos.First(info => info.Name == "Adjust").IsInstalled)
            {
                DrawNotFoundNotice("Adjust SDK");
                return;
            }

            if (!_configData.AdjustEnabled)
            {
                DrawDisabledNotice("Adjust SDK");
                return;
            }

            _adjustFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_adjustFoldout, "App Credentials");
            if (_adjustFoldout)
            {
                EditorGUI.indentLevel++;
                _configData.AdjustAppToken = DrawTextField("App Token", _configData.AdjustAppToken, "From Adjust Dashboard → App Settings");
                _configData.AdjustEnvironment = (AdjustEnvironment)EditorGUILayout.EnumPopup(
                    new GUIContent("Environment", "Use Sandbox for testing, Production for release builds"),
                    _configData.AdjustEnvironment);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space(4);
            GUILayout.Label("Event Tokens", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUI.indentLevel++;
            _configData.AdjustFirstLaunchEventToken = DrawTextField("First Launch", _configData.AdjustFirstLaunchEventToken, "Custom event token from Adjust");
            _configData.AdjustTutorialStartEventToken = DrawTextField("Tutorial Start", _configData.AdjustTutorialStartEventToken, "Custom event token from Adjust");
            _configData.AdjustTutorialCompleteEventToken = DrawTextField("Tutorial Complete", _configData.AdjustTutorialCompleteEventToken, "Custom event token from Adjust");
            _configData.AdjustPurchaseEventToken = DrawTextField("IAP Purchase (Revenue)", _configData.AdjustPurchaseEventToken, "Revenue event token from Adjust — verified IAP purchases report revenue here. Empty = IAP revenue is not sent.");
            EditorGUILayout.PropertyField(_serializedConfigData.FindProperty("AdjustLevelStartEventTokens"));
            EditorGUILayout.PropertyField(_serializedConfigData.FindProperty("AdjustLevelCompleteEventTokens"));
            EditorGUILayout.PropertyField(_serializedConfigData.FindProperty("AdjustLevelFailEventTokens"));
            EditorGUI.indentLevel--;
        }

        private float _lastFirebaseConfigCheckTime;
        private void DrawFirebaseTab()
        {
            bool oldValue = _configData.FirebaseEnabled;
            DrawSdkHeader(4, ref _configData.FirebaseEnabled);
            if (oldValue != _configData.FirebaseEnabled)
            {
                SetDefineSymbol(AnalyticsDefineSymbols.FirebaseSymbol, _configData.FirebaseEnabled, EditorUserBuildSettings.selectedBuildTargetGroup);
                ApplyToAllPlatforms();
            }

            if (!_sdkInfos.First(info => info.Name == "Firebase").IsInstalled)
            {
                DrawNotFoundNotice("Firebase SDK");
                return;
            }

            if (!_configData.FirebaseEnabled)
            {
                DrawDisabledNotice("Firebase SDK");
                return;
            }

            if ((string.IsNullOrEmpty(_configData.FirebaseAndroidConfigPath) || string.IsNullOrEmpty(_configData.FirebaseIOSConfigPath)) &&
                Time.time - _lastFirebaseConfigCheckTime >= 10f)
            {
                _configData.FirebaseAndroidConfigPath = FindPath("google-services", "json");
                _configData.FirebaseIOSConfigPath = FindPath("GoogleService-Info", "plist");
                _lastFirebaseConfigCheckTime = Time.time;
            }

            _firebaseAndroidCredentialsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_firebaseAndroidCredentialsFoldout, "Android");
            if (_firebaseAndroidCredentialsFoldout)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label($"Config: {_configData.FirebaseAndroidConfigPath}", AnalyticsEditorWindowStyles.SubHeader);
                EditorGUI.indentLevel--;
            }
            
            if (!string.IsNullOrEmpty(_configData.FirebaseAndroidConfigPath))
            {
                if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    Object firebaseAndroidConfigData = AssetDatabase.LoadAssetAtPath<Object>(_configData.FirebaseAndroidConfigPath);
                    EditorGUIUtility.PingObject(firebaseAndroidConfigData);
                    Selection.activeObject = firebaseAndroidConfigData;
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();


            _firebaseIosCredentialsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(_firebaseIosCredentialsFoldout, "Ios");
            if (_firebaseIosCredentialsFoldout)
            {
                EditorGUI.indentLevel++;
                GUILayout.Label($"Config: {_configData.FirebaseIOSConfigPath}", AnalyticsEditorWindowStyles.SubHeader);
                EditorGUI.indentLevel--;
            }

            if (!string.IsNullOrEmpty(_configData.FirebaseIOSConfigPath))
            {
                if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition))
                {
                    Object firebaseIosConfigData = AssetDatabase.LoadAssetAtPath<Object>(_configData.FirebaseIOSConfigPath);
                    Event.current.Use();
                    EditorGUIUtility.PingObject(firebaseIosConfigData);
                    Selection.activeObject = firebaseIosConfigData;
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
            
        }

        private string FindPath(string fileName, string extension)
        {
            foreach (string path in AssetDatabase.GetAllAssetPaths())
            {
                if (!path.EndsWith(fileName + "." + extension))
                    continue;
                return path;
            }

            return string.Empty;
        }
        
        private void DrawSettingsTab()
        {
            GUILayout.Label("General Settings", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUILayout.Space(2);
            _configData.InternetRequired = EditorGUILayout.Toggle(new GUIContent("Require Internet", "Require internet for Gameplay. (Not required for offline gameplay.)"), _configData.InternetRequired);
            _configData.EnableInEditor = EditorGUILayout.Toggle(new GUIContent("Enable Analytics in Editor", "Run analytics calls while in Play Mode in the editor"), _configData.EnableInEditor);
            _configData.VerboseLogging = EditorGUILayout.Toggle(new GUIContent("Verbose Logging (Global)", "Enable detailed logging across all SDKs"), _configData.VerboseLogging);

            EditorGUILayout.Space(2);
            DrawHorizontalLine();

            EditorGUILayout.Space(8);
            GUILayout.Label("Export / Import", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUILayout.Space(2);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Config Folder", GUILayout.Height(28)))
                EditorUtility.RevealInFinder(AssetDatabase.GetAssetPath(_configData));
            if (GUILayout.Button("Export to JSON", GUILayout.Height(28)))
                ExportToJson();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            DrawHorizontalLine();
            EditorGUILayout.Space(8);

            GUILayout.Label("State", AnalyticsEditorWindowStyles.SectionTitle);
            EditorGUILayout.Space(2);
            Color prevColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.8f, 0.25f, 0.25f);
            if (GUILayout.Button("Reset All Settings to Defaults", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog("Reset Analytics Config",
                        "This will reset all SDK keys and settings to their defaults. This cannot be undone.", "Reset", "Cancel"))
                {
                    ResetToDefaults();
                    ShowStatus("All settings reset to defaults.", MessageType.Warning);
                }
            }

            GUI.backgroundColor = prevColor;
        }

        private void DrawFooter()
        {
            EditorGUILayout.BeginHorizontal("Toolbar");
            GUILayout.Space(6);

            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.LabelField(_statusMessage);
            else
            {
                GUILayout.Label($"Config: {AssetDatabase.GetAssetPath(_configData)}", AnalyticsEditorWindowStyles.SubHeader);
                Rect lastRect = GUILayoutUtility.GetLastRect();

                if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
                {
                    Event.current.Use();
                    EditorGUIUtility.PingObject(_configData);
                    Selection.activeObject = _configData;
                }
            }

            GUILayout.FlexibleSpace();

            _configData.AutoSave = GUILayout.Toggle(_configData.AutoSave, "Auto Save");
            if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(54)))
            {
                SaveConfig();
                // ShowStatus("Configuration saved.", MessageType.Info);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSdkHeader(int sdkIndex, ref bool enabled)
        {
            SdkInfo sdk = _sdkInfos[sdkIndex];

            EditorGUILayout.BeginHorizontal();

            Rect iconRect = GUILayoutUtility.GetRect(6, 32, GUILayout.Width(6));
            EditorGUI.DrawRect(iconRect, enabled ? sdk.AccentColor : new Color(0.3f, 0.3f, 0.3f));

            GUILayout.Space(10);

            EditorGUILayout.BeginVertical();
            GUILayout.Label(sdk.Name, AnalyticsEditorWindowStyles.SectionTitle);
            GUILayout.Label($"v{sdk.Version} — {sdk.Description}", AnalyticsEditorWindowStyles.SubHeader);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();
            if (!sdk.IsInstalled)
                GUI.enabled = false;
            enabled = EditorGUILayout.Toggle(enabled, GUILayout.Width(20));
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);
            DrawHorizontalLine();
            EditorGUILayout.Space(6);
        }

        private void DrawDisabledNotice(string sdkName)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox($"{sdkName} is disabled. Toggle the switch above to configure it.", MessageType.None);
        }

        private void DrawNotFoundNotice(string sdkName)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox($"{sdkName} is not found! Please install the sdk to configure it.", MessageType.None);
        }

        private string DrawTextField(string label, string value, string tooltip = "")
        {
            return EditorGUILayout.TextField(new GUIContent(label, tooltip), value);
        }

        private void DrawHorizontalLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
        }

        private void DrawValidationBox((bool valid, string message)[] checks)
        {
            bool allValid = true;
            foreach ((bool valid, string _) in checks)
                if (!valid)
                {
                    allValid = false;
                    break;
                }

            if (allValid)
            {
                EditorGUILayout.HelpBox("✓ All required fields are configured.", MessageType.None);
            }
            else
            {
                foreach ((bool valid, string message) in checks)
                    if (!valid)
                        EditorGUILayout.HelpBox($"✗ {message}", MessageType.Warning);
            }
        }

        private void LoadOrCreateConfig()
        {
            string[] guids = AssetDatabase.FindAssets("t:AnalyticsConfig");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _configData = AssetDatabase.LoadAssetAtPath<AnalyticsConfig>(path);
                if (_configData != null)
                {
                    _serializedConfigData = new SerializedObject(_configData);
                    return;
                }
            }

            const string dir = "Assets/Settings/Analytics";
            const string assetPath = dir + "/AnalyticsConfig.asset";

            if (!AssetDatabase.IsValidFolder("Assets/Settings"))
                AssetDatabase.CreateFolder("Assets", "Settings");
            if (!AssetDatabase.IsValidFolder(dir))
                AssetDatabase.CreateFolder("Assets/Settings", "Analytics");

            _configData = CreateInstance<AnalyticsConfig>();
            AssetDatabase.CreateAsset(_configData, assetPath);
            AssetDatabase.SaveAssets();
            _serializedConfigData = new SerializedObject(_configData);

            ShowStatus($"Created new config at {assetPath}", MessageType.Info);
        }

        private void ResetToDefaults()
        {
            _configData.AutoSave = true;
            
            // Ad Service
            _configData.AdsEnabled = true;
            _configData.InterstitialEnabled = false;
            _configData.RewardedEnabled = true;
            _configData.BannerEnabled = false;
            _configData.AdPresets.Clear();
            _configData.GlobalAdConditions.Clear();
            
            // Facebook
            _configData.FacebookEnabled = false;
            _configData.FacebookAppId = string.Empty;
            _configData.FacebookClientToken = string.Empty;

            // Applovin
            _configData.ApplovinEnabled = false;
            _configData.ApplovinSdkKey = string.Empty;
            _configData.AndroidInterstitialAdUnitId = string.Empty;
            _configData.AndroidRewardedAdUnitId = string.Empty;
            _configData.AndroidBannerAdUnitId = string.Empty;
            _configData.IosInterstitialAdUnitId = string.Empty;
            _configData.IosRewardedAdUnitId = string.Empty;
            _configData.IosBannerAdUnitId = string.Empty;

            // GameAnalytics
            _configData.GameAnalyticsEnabled = false;
            _configData.GameAnalyticsAndroidGameKey = string.Empty;
            _configData.GameAnalyticsAndroidSecretKey = string.Empty;
            _configData.GameAnalyticsIosGameKey = string.Empty;
            _configData.GameAnalyticsIosSecretKey = string.Empty;
            _configData.GameAnalyticsBuild = false;
            _configData.GameAnalyticsBuildVersion = "0.1.0";

            // Adjust
            _configData.AdjustEnabled = false;
            _configData.AdjustAppToken = string.Empty;
            _configData.AdjustEnvironment = AdjustEnvironment.Sandbox;
            _configData.AdjustFirstLaunchEventToken = string.Empty;
            _configData.AdjustTutorialStartEventToken = string.Empty;
            _configData.AdjustTutorialCompleteEventToken = string.Empty;
            _configData.AdjustLevelStartEventTokens.Clear();
            _configData.AdjustLevelCompleteEventTokens.Clear();
            _configData.AdjustLevelFailEventTokens.Clear();

            // Firebase
            _configData.FirebaseEnabled = false;

            // General Settings
            _configData.EnableInEditor = false;
            _configData.VerboseLogging = false;

            // General Settings
            EditorUtility.SetDirty(_configData);
            SaveConfig();
            ShowStatus("All settings reset to defaults", MessageType.Info);
        }

        private void ShowStatus(string message, MessageType type, float durationSeconds = 3f)
        {
            _statusMessage = message;
            _statusType = type;
            _statusHideDuration = EditorApplication.timeSinceStartup + durationSeconds;
        }

        private void DrawNoConfigState()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.HelpBox("No AnalyticsConfig asset found. Click below to create one.", MessageType.Info);
            if (GUILayout.Button("Create Analytics Config Asset", GUILayout.Height(32)))
                LoadOrCreateConfig();
        }

        private void ExportToJson()
        {
            string json = JsonUtility.ToJson(_configData, true);
            string path = EditorUtility.SaveFilePanel("Export Analytics Config", Application.dataPath, "analytics_config", "json");
            if (!string.IsNullOrEmpty(path))
            {
                File.WriteAllText(path, json);
                ShowStatus($"Exported to {path}", MessageType.Info);
            }
        }

        private void CheckInstalledPackages()
        {
            // GameAnalytics
            _isGameAnalyticsInstalled = TypeExists("GameAnalyticsSDK.GameAnalytics") ||
                                        DirectoryExists("Assets/GameAnalytics") ||
                                        DirectoryExists("Packages/com.gameanalytics.sdk");
            _sdkInfos.First(skdInfo => skdInfo.Name == "GameAnalytics").IsInstalled = _isGameAnalyticsInstalled;
            if (!_isGameAnalyticsInstalled)
                _configData.GameAnalyticsEnabled = false;
#if GAMEANALYTICS_ENABLED
            if (_isGameAnalyticsInstalled)
                _sdkInfos.First(skdInfo => skdInfo.Name == "GameAnalytics").Version = Settings.VERSION;
#endif

            // Firebase
            _isFirebaseInstalled = TypeExists("Firebase.Analytics.FirebaseAnalytics") ||
                                   DirectoryExists("Assets/Firebase") ||
                                   FileExists("Assets/Firebase/m2repository/com/google/firebase/firebase-analytics");
            _sdkInfos.First(skdInfo => skdInfo.Name == "Firebase").IsInstalled = _isFirebaseInstalled;
            if (!_isFirebaseInstalled)
                _configData.FirebaseEnabled = false;

#if FIREBASE_ENABLED
            if (_isFirebaseInstalled)
            {
                _configData.FirebaseAndroidConfigPath = FindPath("google-services", "json");
                _configData.FirebaseIOSConfigPath = FindPath("GoogleService-Info", "plist");

                const string packageJsonPath = "Assets/Firebase/m2repository/com/google/firebase/firebase-analytics-unity/maven-metadata.xml";
                if (File.Exists(packageJsonPath))
                {
                    string[] json = File.ReadAllLines(packageJsonPath);
                    string versionText = json.First(line => line.Contains("<versions><version>"));
                    versionText = versionText.Trim().Remove(0, "<versions><version>".Length).Remove(versionText.IndexOf("<", StringComparison.Ordinal) + 2);
                    _sdkInfos.First(info => info.Name == "Firebase").Version = versionText;
                }
            }
#endif

            // Adjust
            _isAdjustInstalled = TypeExists("com.adjust.sdk.Adjust") ||
                                 TypeExists("AdjustSdk.Adjust") ||
                                 DirectoryExists("Assets/Adjust") ||
                                 DirectoryExists("Packages/com.adjust.sdk") ||
                                 FileExists("Assets/Adjust/Unity/Adjust.cs") ||
                                 FileExists("Assets/Plugins/Android/adjust-android.jar");

            _sdkInfos.First(skdInfo => skdInfo.Name == "Adjust").IsInstalled = _isAdjustInstalled;
            if (!_isAdjustInstalled)
                _configData.AdjustEnabled = false;
#if ADJUST_ENABLED
            if (_isAdjustInstalled)
            {
                const string packageJsonPath = "Assets/Adjust/package.json";
                if (File.Exists(packageJsonPath))
                {
                    string[] json = File.ReadAllLines(packageJsonPath);
                    string versionText = json.First(line => line.Contains("version"));
                    versionText = versionText.Trim().Remove(0, "\"version:\"".Length).Replace("\"", "").Replace(",", "").Trim();
                    _sdkInfos.First(info => info.Name == "Adjust").Version = versionText;
                }
            }
#endif

            // Facebook
            _isFacebookInstalled = TypeExists("Facebook.Unity.FB") ||
                                   TypeExists("Facebook.Unity.FacebookSettings") ||
                                   DirectoryExists("Assets/FacebookSDK") ||
                                   DirectoryExists("Packages/com.facebook.unity.sdk") ||
                                   FileExists("Assets/FacebookSDK/SDK/Scripts/FB.cs") ||
                                   AssetDatabase.FindAssets("FacebookSettings").Length > 0 ||
                                   FileExists("Assets/Plugins/Android/facebook-android-sdk") ||
                                   DirectoryExists("Assets/Plugins/iOS/FBSDKCoreKit.framework");

            _sdkInfos.First(skdInfo => skdInfo.Name == "Facebook").IsInstalled = _isFacebookInstalled;
            if (!_isFacebookInstalled)
                _configData.FacebookEnabled = false;
#if FACEBOOK_ENABLED
            if (_isFacebookInstalled)
                _sdkInfos.First(info => info.Name == "Facebook").Version = FacebookSdkVersion.Build;
#endif

            // Applovin Max
            _isMaxInstalled = TypeExists("MaxSdk") ||
                              TypeExists("MaxSdkBase") ||
                              TypeExists("AppLovinSettings") ||
                              DirectoryExists("Assets/MaxSdk") ||
                              DirectoryExists("Assets/AppLovin") ||
                              DirectoryExists("Packages/com.applovin.max") ||
                              FileExists("Assets/MaxSdk/Scripts/MaxSdk.cs") ||
                              FileExists("Assets/MaxSdk/AppLovin/Scripts/MaxSdk.cs") ||
                              AssetDatabase.FindAssets("AppLovinSettings").Length > 0 ||
                              FileExists("Assets/Plugins/Android/AppLovinSdk.androidlib") ||
                              FileExists("Assets/MaxSdk/Plugins/Android/AppLovinSdk.androidlib") ||
                              DirectoryExists("Assets/Plugins/iOS/AppLovinSDK.framework") ||
                              DirectoryExists("Assets/MaxSdk/Plugins/iOS/AppLovinSDK.framework") ||
                              TypeExists("AppLovinIntegrationManager") ||
                              TypeExists("AppLovinEditorCoroutine");

            _sdkInfos.First(skdInfo => skdInfo.Name == "Applovin MAX").IsInstalled = _isMaxInstalled;
            if (!_isMaxInstalled)
                _configData.ApplovinEnabled = false;
#if MAX_ENABLED
            if (_isMaxInstalled)
                _sdkInfos.First(info => info.Name == "Applovin MAX").Version = AppLovinIntegrationManager.LoadPluginDataSync().AppLovinMax.CurrentVersions.Unity;
#endif
            // Hammer
            _isHammerInstalled = DirectoryExists("Assets/UDO/HammerSDK");

            // Repaint();
        }

        private void RemoveAllAnalyticsSymbols()
        {
            BuildTargetGroup targetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            List<string> defines = GetCurrentDefines(targetGroup);

            foreach (string symbol in AllAnalyticsSymbols)
            {
                defines.Remove(symbol);
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", defines));
        }

        private List<string> GetCurrentDefines(BuildTargetGroup targetGroup)
        {
            string definesString = PlayerSettings.GetScriptingDefineSymbolsForGroup(targetGroup);
            return definesString.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        }

        private void SetDefineSymbol(string symbol, bool enable, BuildTargetGroup targetGroup)
        {
            List<string> defines = GetCurrentDefines(targetGroup);

            if (enable)
            {
                if (!defines.Contains(symbol))
                {
                    defines.Add(symbol);
                    if (_configData.VerboseLogging)
                        Debug.Log($"[Analytics] Enabled {symbol}");
                }
            }
            else
            {
                if (defines.Contains(symbol))
                {
                    defines.Remove(symbol);
                    if (_configData.VerboseLogging)
                        Debug.Log($"[Analytics] Disabled {symbol}");
                }
            }

            PlayerSettings.SetScriptingDefineSymbolsForGroup(targetGroup, string.Join(";", defines));
        }

        private void ApplyToAllPlatforms()
        {
            BuildTargetGroup currentGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
            List<string> currentDefines = GetCurrentDefines(currentGroup);

            List<string> analyticsDefines = currentDefines.Where(d => AllAnalyticsSymbols.Contains(d)).ToList();

            BuildTargetGroup[] platforms = new[]
            {
                BuildTargetGroup.Standalone,
                BuildTargetGroup.iOS,
                BuildTargetGroup.Android,
            };

            foreach (BuildTargetGroup platform in platforms)
            {
                if (platform == BuildTargetGroup.Unknown) continue;

                List<string> platformDefines = GetCurrentDefines(platform);

                foreach (string symbol in AllAnalyticsSymbols)
                {
                    platformDefines.Remove(symbol);
                }

                platformDefines.AddRange(analyticsDefines);

                PlayerSettings.SetScriptingDefineSymbolsForGroup(platform, string.Join(";", platformDefines));
            }
        }

        private bool TypeExists(string typeName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetType(typeName) != null);
        }

        private bool DirectoryExists(string path)
        {
            return Directory.Exists(Path.Combine(Application.dataPath.Replace("/Assets", ""), path));
        }

        private bool FileExists(string path)
        {
            return File.Exists(Path.Combine(Application.dataPath.Replace("/Assets", ""), path));
        }

        private void SaveConfig()
        {
            _lastSaveTime = Time.time;
            EditorUtility.SetDirty(_configData);
            AssetDatabase.SaveAssets();
            SyncConfig();
            AssetDatabase.Refresh();
            ShowStatus("Configuration saved.", MessageType.Info);
        }

        private void SyncConfig()
        {
#if FACEBOOK_ENABLED
            if (FacebookSettings.NullableInstance == null)
            {
                FacebookSettings instance = CreateInstance<FacebookSettings>();
                string path1 = Path.Combine(Application.dataPath, "FacebookSDK/SDK/Resources");
                if (!Directory.Exists(path1))
                    Directory.CreateDirectory(path1);
                string fullPath = Path.Combine(Path.Combine("Assets", "FacebookSDK/SDK/Resources"), "FacebookSettings.asset");
                AssetDatabase.CreateAsset(instance, fullPath);
            }

            FacebookSettings.AppIds[0] = _configData.FacebookAppId;
            FacebookSettings.AppLabels[0] = _configData.FacebookAppName;
            FacebookSettings.ClientTokens[0] = _configData.FacebookClientToken;
            EditorUtility.SetDirty(FacebookSettings.Instance);
            ManifestMod.GenerateManifest();
#endif

#if GAMEANALYTICS_ENABLED
            Settings gameAnalyticsSettings = AssetDatabase.LoadAssetAtPath<Settings>(Path.Combine("Assets", "Resources", "GameAnalytics", "Settings.asset"));
            for (int index = gameAnalyticsSettings.Platforms.Count; index > 0; index--)
            {
                gameAnalyticsSettings.RemovePlatformAtIndex(index - 1);
            }

            gameAnalyticsSettings.AddPlatform(RuntimePlatform.Android);
            gameAnalyticsSettings.UpdateGameKey(0, _configData.GameAnalyticsAndroidGameKey);
            gameAnalyticsSettings.UpdateSecretKey(0, _configData.GameAnalyticsAndroidSecretKey);

            gameAnalyticsSettings.AddPlatform(RuntimePlatform.IPhonePlayer);
            gameAnalyticsSettings.UpdateGameKey(1, _configData.GameAnalyticsIosGameKey);
            gameAnalyticsSettings.UpdateSecretKey(1, _configData.GameAnalyticsIosSecretKey);
            gameAnalyticsSettings.UsePlayerSettingsBuildNumber = true;
            gameAnalyticsSettings.SubmitFpsAverage = true;
            gameAnalyticsSettings.SubmitFpsCritical = true;
            gameAnalyticsSettings.SubmitErrors = true;
            gameAnalyticsSettings.NativeErrorReporting = true;
            gameAnalyticsSettings.InfoLogBuild = false;
            gameAnalyticsSettings.InfoLogEditor = false;
            gameAnalyticsSettings.VerboseLogBuild = false;
            EditorUtility.SetDirty(GameAnalytics.SettingsGA);
#endif

#if ADJUST_ENABLED
#endif

#if MAX_ENABLED

            AppLovinSettings.Instance.SdkKey = _configData.ApplovinSdkKey;
            AppLovinInternalSettings.Instance.ConsentFlowEnabled = _configData.PrivacyFlowEnabled;
            AppLovinInternalSettings.Instance.ConsentFlowPrivacyPolicyUrl = _configData.PrivacyTermsURL;
            AppLovinInternalSettings.Instance.ShouldShowTermsAndPrivacyPolicyAlertInGDPR = _configData.PrivacyFlowEnabledInGDPR;
            EditorUtility.SetDirty(AppLovinInternalSettings.Instance);
            EditorUtility.SetDirty(AppLovinSettings.Instance);
            AppLovinInternalSettings.Instance.Save();
#endif

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
