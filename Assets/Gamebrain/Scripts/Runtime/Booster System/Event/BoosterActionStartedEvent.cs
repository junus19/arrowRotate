using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class BoosterActionStartedEvent : IEvent
    {
        private readonly BoosterType _boosterType;

        public BoosterType BoosterType => _boosterType;
        
        public BoosterActionStartedEvent(BoosterType boosterType)
        {
            _boosterType = boosterType;
        }
    }
}
