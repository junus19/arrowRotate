#if HAMMER_ENABLED
using System.Threading.Tasks;
using System.Collections.Generic;
using Udo.HammerSDK.Runtime._Hammer;

namespace Gamebrain.SDK
{
    public class HammerSDKModule : IAnalyticModule
    {
        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;
        
        public Task Initialize()
        {
            _isInitialized = true;
            return Task.CompletedTask;
        }

        public void SetUserId(string userId)
        {
        }

        public void SetUserProperty(string propertyName, string value)
        {
        }

        public void SendCustomEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            Hammer.Instance.ANALYTICS_CustomEvent(eventName, parameters);
        }

        public void SendError(string errorMessage, string errorType = "")
        {
        }

        public void SendLevelStartEvent(int level, string levelName = "")
        {
            Hammer.Instance.ANALYTICS_ProgressionEvents.LevelStart(level);
        }

        public void SendLevelCompleteEvent(int level, string levelName = "", float completionTime = 0)
        {
            Hammer.Instance.ANALYTICS_ProgressionEvents.LevelComplete(level);
        }

        public void SendLevelFailEvent(int level, FailType failType, string levelName = "", string failReason = "", float levelDuration = 0f)
        {
            Hammer.Instance.ANALYTICS_ProgressionEvents.LevelFail(level);
        }

        public void SendPurchaseEvent(string itemId, string currency, float amount, string transactionId = "")
        {
            Hammer.Instance.ANALYTICS_GameEconomyEvents.Spent_Hard_Currency((int)amount, currency, itemId);
        }

        public void SendAdImpressionEvent(string adType, string adPlacement, string adNetwork = "")
        {
        }

        public void SendAdClickEvent(string adType, string adPlacement, string adNetwork = "")
        {
        }
    }
}
#endif
