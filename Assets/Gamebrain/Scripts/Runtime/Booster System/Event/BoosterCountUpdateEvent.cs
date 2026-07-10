using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class BoosterCountUpdateEvent : IEvent
    {
        public BoosterType BoosterType;
        public int BoosterCount;

        public BoosterCountUpdateEvent(BoosterType boosterType, int boosterCount)
        {
            BoosterType = boosterType;
            BoosterCount = boosterCount;
        }
    }
}
