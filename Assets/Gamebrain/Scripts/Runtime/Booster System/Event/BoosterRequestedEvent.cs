using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class BoosterRequestedEvent : IEvent
    {
        private readonly BoosterType _boosterType;

        public BoosterType BoosterType => _boosterType;
        
        public BoosterRequestedEvent(BoosterType boosterType)
        {
            _boosterType = boosterType;
        }
    }
}
