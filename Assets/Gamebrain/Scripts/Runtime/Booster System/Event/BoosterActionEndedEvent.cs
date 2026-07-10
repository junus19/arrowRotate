using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class BoosterActionEndedEvent : IEvent
    {
        private readonly BoosterType _boosterType;

        public BoosterType BoosterType => _boosterType;
        
        public BoosterActionEndedEvent(BoosterType boosterType)
        {
            _boosterType = boosterType;
        }
    }
}
