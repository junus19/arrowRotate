using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class SettingChangeRequestEvent : IEvent
    {
        private readonly SettingType _settingType;
        
        public SettingType SettingType => _settingType;

        public SettingChangeRequestEvent(SettingType settingType)
        {
            _settingType = settingType;
        }
    }
}
