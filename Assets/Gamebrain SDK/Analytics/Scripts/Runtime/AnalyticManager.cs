using UnityEngine;
using GameBrain.Casual;
using System.Collections;
using System.Threading.Tasks;
using GameBrain.SDK.Monetization;
using UnityEngine.SceneManagement;

#if ADJUST_ENABLED
using AdjustSdk;
#endif

#if GAMEANALYTICS_ENABLED
using GameAnalyticsSDK;
#endif

namespace GameBrain.SDK
{
    [DisallowMultipleComponent]
    public class AnalyticManager : MonoBehaviour
    {
        [SerializeField] private AnalyticsConfig _config;
        [SerializeField] private GameData _gameData;
        private InitializationIndicator _initializationIndicator;
        private Camera _camera;
        private AdService _adService;
        private AnalyticsService _analyticsService;
        private RemoteConfigService _remoteConfigService;
        private bool _hasActiveInternetConnection;

        public AdService ADService => _adService;
        public AnalyticsService AnalyticsService => _analyticsService;

        private void Awake()
        {
            _analyticsService = new AnalyticsService
            (
                _config
#if GAMEANALYTICS_ENABLED
                , new GameAnalyticsModule(_config)
#else
                , null
#endif

#if FIREBASE_ENABLED
                ,new FirebaseModule(_config)
#else
                , null
#endif

#if ADJUST_ENABLED
                , new AdjustModule(_config)
#else
                , null
#endif

#if HAMMER_ENABLED
                , new HammerSDKModule()
#else
                , null
#endif

#if FACEBOOK_ENABLED
                , new FacebookModule()
#else
                , null
#endif
            );
            _adService = new AdService(_config);
            _remoteConfigService = new RemoteConfigService(_config);
            StartCoroutine(InitializeRoutine());
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
        }

        private IEnumerator InitializeRoutine()
        {
            int progress = 0;
            _camera = FindAnyObjectByType<Camera>();
            _initializationIndicator = FindAnyObjectByType<InitializationIndicator>();
            if (_config.InternetRequired)
            {
                while (Application.internetReachability != NetworkReachability.NotReachable)
                {
                    yield return new WaitForSeconds(0.1f);
                }
            }

            Task analyticServiceInitialize = _analyticsService.Initialize();
            progress = 5;
            SetProgress(progress);
            yield return new WaitUntil(() => analyticServiceInitialize.IsCompletedSuccessfully);
            progress = 10;
            SetProgress(progress);

            // if (!Application.isEditor)
            // {
            //     Task connectionCheckTask = NetworkService.CheckInternetConnection((connected) => _hasActiveInternetConnection = connected);
            //     yield return new WaitUntil(() => connectionCheckTask.IsCompletedSuccessfully);
            //
            //     if (_hasActiveInternetConnection && _config.GameAnalyticsEnabled)
            //     {
            //         yield return StartCoroutine(_remoteConfigService.Initialize());
            //     }
            // }

            progress = 15;
            SetProgress(progress);
            
            Task adServiceInitialize = _adService.Initialize();
            progress = 20;
            SetProgress(progress);
            
            yield return new WaitUntil(() => adServiceInitialize.IsCompletedSuccessfully);
            progress = 25;
            SetProgress(progress);

            do
            {
                yield return new WaitForSeconds(0.1f);
                progress += 5;
                SetProgress(progress);
            } while (progress < 100);

            yield return new WaitForSeconds(0.25f);

            DontDestroyOnLoad(gameObject);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

            if (_config.AdsEnabled && _config.InterstitialEnabled)
                StartCoroutine(TimeBasedAdRoutine());
        }

        private void SetProgress(int percentage)
        {
            _initializationIndicator.SetProgressText(percentage);//($"{percentage}%");
        }

        public void OnLevelCompleted(int level)
        {
            // if (_storeData.IsNoAdsPurchased()) return;
            bool globalConditionsAreMet = !_analyticsService.Config.HasGlobalAdConditions()
                                          || (_analyticsService.Config.HasGlobalAdConditions()
                                              && _analyticsService.Config.GlobalAdConditionsAreMet(_analyticsService.SessionCount, level,
                                                  Time.time - _adService.LastInterstitialShowTime));
            if (!globalConditionsAreMet) return;
            bool isThereAdPreset = _analyticsService.Config.HasLevelBasedAdPreset();
            if (isThereAdPreset)
            {
                AdPreset adPreset = _analyticsService.Config.GetLevelBasedAdPreset(_analyticsService.SessionCount);
                bool levelIndexMeetsPresetCriteria = level % adPreset.Frequency == 0;
                bool adPresetConditionsAreMet = !adPreset.HasConditions() || adPreset.ConditionsMet(_analyticsService.SessionCount,
                    level, Time.time - _adService.LastInterstitialShowTime);
                bool adCanBeShowed = isThereAdPreset && levelIndexMeetsPresetCriteria && adPresetConditionsAreMet && globalConditionsAreMet;
                if (adCanBeShowed)
                {
                    _adService.ShowInterstitialAd();
                    _analyticsService.SendCustomEvent("Inter:" + level);
                }
            }
            else
            {
                _adService.ShowInterstitialAd();
                _analyticsService.SendCustomEvent("Inter:" + level);
            }
        }

        private IEnumerator TimeBasedAdRoutine()
        {
            if ( /*_storeData.IsNoAdsPurchased() || */!_config.HasTimeBasedAdPreset()) yield break;
            AdPreset adPreset = _analyticsService.Config.GetTimeBasedAdPreset(_analyticsService.SessionCount);
            do
            {
                bool globalConditionsAreMet = !_analyticsService.Config.HasGlobalAdConditions()
                                              || (_analyticsService.Config.HasGlobalAdConditions()
                                                  && _analyticsService.Config.GlobalAdConditionsAreMet(_analyticsService.SessionCount, _gameData.GetLevelIndex(),
                                                      Time.time - _adService.LastInterstitialShowTime));
                if (!globalConditionsAreMet) yield return null;
                yield return new WaitForSeconds(adPreset.Frequency);
                bool adPresetConditionsAreMet = !adPreset.HasConditions() ||
                                                adPreset.ConditionsMet(_analyticsService.SessionCount, _gameData.GetLevelIndex(),
                                                    Time.time - _adService.LastInterstitialShowTime);
                bool canShowInter = adPresetConditionsAreMet && globalConditionsAreMet;
                if (!canShowInter) continue;

                _adService.ShowInterstitialAd();
                _analyticsService.SendCustomEvent("Inter:" + _gameData.GetAnalyticLevelIndex());
            } while (true);
        }
    }
}
