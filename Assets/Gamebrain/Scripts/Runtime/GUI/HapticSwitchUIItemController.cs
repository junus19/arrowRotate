using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class HapticSwitchUIItemController : SwitchUIItemController
    {
        public EventBinding<HapticStatusIsChangedEvent> _hapticStatusChangedEvent;

        protected override void Awake()
        {
            base.Awake();
        }

        public override void Init(bool status)
        {
            base.Init(status);

            _hapticStatusChangedEvent = new EventBinding<HapticStatusIsChangedEvent>(OnHapticStatusChangedEvent);
            EventBus<HapticStatusIsChangedEvent>.Register(_hapticStatusChangedEvent);
        }

        protected override void OnSwitchHandleButtonClicked()
        {
            EventBus<SettingChangeRequestEvent>.Raise(new SettingChangeRequestEvent(SettingType.Haptic));
        }

        private void OnHapticStatusChangedEvent(HapticStatusIsChangedEvent eventnfo)
        {
            SwitchUIItem.ChangeStatus(eventnfo.Status, true);
        }

    }
}
