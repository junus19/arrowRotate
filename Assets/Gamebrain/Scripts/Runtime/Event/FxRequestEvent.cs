using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class FxRequestEvent : IEvent
    {
        private readonly EffectType _effectType;
        
        public EffectType EffectType => _effectType;

        public FxRequestEvent(EffectType effectType)
        {
            _effectType = effectType;
        }
    }
}
