using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class HapticStatusIsChangedEvent : IEvent
    {
        private readonly bool _status;
        
        public bool Status => _status;

        public HapticStatusIsChangedEvent(bool status)
        {
            _status = status;
        }
    }
}
