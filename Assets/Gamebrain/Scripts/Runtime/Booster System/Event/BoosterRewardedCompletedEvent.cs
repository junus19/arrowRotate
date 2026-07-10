using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class BoosterRewardedCompletedEvent : IEvent
    {
        private readonly BoosterType _boosterType;

        public BoosterType BoosterType => _boosterType;
        
        public BoosterRewardedCompletedEvent(BoosterType boosterType)
        {
            _boosterType = boosterType;
        }
    }
}
