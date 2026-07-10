using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class AudioSwitchUIItemController : SwitchUIItemController
    {
        public EventBinding<AudioStatusIsChangedEvent> _audioStatusChangedEvent;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void Init(bool status)
        {
            base.Init(status);

            _audioStatusChangedEvent = new EventBinding<AudioStatusIsChangedEvent>(OnAudioStatusChangedEvent);
            EventBus<AudioStatusIsChangedEvent>.Register(_audioStatusChangedEvent);

        }
        protected override void OnSwitchHandleButtonClicked()
        {
            EventBus<SettingChangeRequestEvent>.Raise(new SettingChangeRequestEvent(SettingType.Audio));
        }

        private void OnAudioStatusChangedEvent(AudioStatusIsChangedEvent eventnfo)
        {
            SwitchUIItem.ChangeStatus(eventnfo.Status, true);
        }
    }
}
