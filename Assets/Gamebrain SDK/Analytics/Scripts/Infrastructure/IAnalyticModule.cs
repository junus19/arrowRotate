using System.Threading.Tasks;
using System.Collections.Generic;

namespace GameBrain.SDK
{
    public interface IAnalyticModule
    {
        bool IsInitialized { get; }
        
        Task Initialize();
        
        void SetUserId(string userId);
        void SetUserProperty(string propertyName, string value);
        
        void SendCustomEvent(string eventName, Dictionary<string, object> parameters = null);
        void SendError(string errorMessage, string errorType = "");
        
        void SendLevelStartEvent(int level, string levelName = "");
        void SendLevelCompleteEvent(int level, string levelName = "", float completionTime = 0f);
        void SendLevelFailEvent(int level, FailType failType, string levelName = "", string failReason = "", float levelDuration = 0f);
        void SendPurchaseEvent(string itemId, string currency, float amount, string transactionId = "");
        void SendAdImpressionEvent(AdType adType, string adPlacement, string adNetwork = "");
        void SendAdClickEvent(string adPlacement, string adNetwork = "");
        void SendResourceEvent(ResourceFlowType flowType, string currency, float amount, string resourceType, string resourceId);
    }

    public enum AdType
    {
        Banner,
        Interstitial,
        Rewarded
    }

    public enum ResourceFlowType
    {
        Gain,
        Spend
    }
}
