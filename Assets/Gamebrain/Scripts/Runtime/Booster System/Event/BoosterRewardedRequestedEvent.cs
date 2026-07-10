using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class BoosterRewardedRequestedEvent : IEvent
    {
        private readonly BoosterType _boosterType;

        public BoosterType BoosterType => _boosterType;
        
        public BoosterRewardedRequestedEvent(BoosterType boosterType)
        {
            _boosterType = boosterType;
        }
    }
}
