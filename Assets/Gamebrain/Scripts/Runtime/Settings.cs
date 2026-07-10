using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class Settings
    {
        private EventBinding<SettingChangeRequestEvent> settingChangeRequestEvent;
        private SettingsData settingsData;

        public Settings(SettingsData settingsData)
        {
            this.settingsData = settingsData;
            settingChangeRequestEvent = new EventBinding<SettingChangeRequestEvent>(OnSettingChangeRequest);
            EventBus<SettingChangeRequestEvent>.Register(settingChangeRequestEvent);
        }

        private void OnSettingChangeRequest(SettingChangeRequestEvent eventInfo)
        {
            SettingType s = eventInfo.SettingType;

            if (eventInfo.SettingType == SettingType.Audio)
                SetStatusOfAudio();
            else if (eventInfo.SettingType == SettingType.Haptic)
                SetStatusOfHaptic();
        }

        private void SetStatusOfHaptic()
        {
            settingsData.SetHapticStatus();
            EventBus<HapticStatusIsChangedEvent>.Raise(new HapticStatusIsChangedEvent(settingsData.GetHapticStatus()));
            EventBus<SettingIsChangedEvent>.Raise(new SettingIsChangedEvent(SettingType.Haptic, settingsData.GetHapticStatus()));
        }

        private void SetStatusOfAudio()
        {
            settingsData.SetAudioFxStatus();
            EventBus<AudioStatusIsChangedEvent>.Raise(new AudioStatusIsChangedEvent(settingsData.GetAudioFxStatus()));
            EventBus<SettingIsChangedEvent>.Raise(new SettingIsChangedEvent(SettingType.Audio, settingsData.GetAudioFxStatus()));
        }
    }

    public enum SettingType
    {
        Default,
        Audio,
        Haptic
    }
}
