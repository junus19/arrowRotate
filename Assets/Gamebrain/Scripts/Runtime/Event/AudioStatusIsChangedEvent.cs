using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class AudioStatusIsChangedEvent : IEvent
    {
        private readonly bool _status;
        
        public bool Status => _status;

        public AudioStatusIsChangedEvent(bool status)
        {
            _status = status;
        }
    }
}
