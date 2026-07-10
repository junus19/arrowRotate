using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class BoosterActiveStatusEvent : IEvent
    {
        public BoosterType BoosterType;
        public int BoosterCount;
        public bool IsActive;

        public BoosterActiveStatusEvent(BoosterType boosterType, bool isActive, int boosterCount)
        {
            BoosterType = boosterType;
            IsActive = isActive;
            BoosterCount = boosterCount;
        }
    }
}
