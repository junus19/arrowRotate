using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class SettingIsChangedEvent : IEvent
    {
        private readonly SettingType _settingType;
        public SettingType SettingType => _settingType;

        private readonly bool _status;
        public bool Status => _status;

        public SettingIsChangedEvent(SettingType settingType, bool status)
        {
            _settingType = settingType;
            _status = status;
        }
    }
}
