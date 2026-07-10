using GameBrain.Utils;

namespace GameBrain.Casual
{
    public class FeedbackManager
    {
        private AudioManager audioManager;
        private HapticManager hapticManager;

        private EventBinding<SettingIsChangedEvent> settingIsChangedEvent;
        private EventBinding<FxRequestEvent> fxRequestedEventBinding;

        public FeedbackManager(Feedbacks_SO feedbacks_SO, SettingsData settingsData)
        {
            audioManager = new AudioManager(feedbacks_SO, settingsData);
            hapticManager = new HapticManager(feedbacks_SO, settingsData);

            settingIsChangedEvent = new EventBinding<SettingIsChangedEvent>(OnSettingIsChanged);
            EventBus<SettingIsChangedEvent>.Register(settingIsChangedEvent);

            fxRequestedEventBinding = new EventBinding<FxRequestEvent>(OnFxRequested);
            EventBus<FxRequestEvent>.Register(fxRequestedEventBinding);
        }

        public void Init()
        {
            audioManager.Init();
            hapticManager.Init();
        }

        private void OnSettingIsChanged(SettingIsChangedEvent eventInfo)
        {
            if (eventInfo.SettingType == SettingType.Audio)
                audioManager.AudioStatusIsChanged(eventInfo.Status);
            else if (eventInfo.SettingType == SettingType.Haptic)
                hapticManager.HapticStatusIsChanged(eventInfo.Status);
        }

        private void OnFxRequested(FxRequestEvent eventInfo)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log("Fx : " + eventInfo.EffectType);
#endif
            audioManager.PlayAudioClip(eventInfo.EffectType);
            hapticManager.PlayHaptic(eventInfo.EffectType);
        }
    }
}
