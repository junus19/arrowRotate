using UnityEngine;
using GameBrain.Utils;
using Lofelt.NiceVibrations;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class HapticManager
    {
        private Feedbacks_SO feedbacks_SO;
        private SettingsData settingsData;
        private Dictionary<EffectType, HapticPatterns.PresetType> hapticFxDictionary = new Dictionary<EffectType, HapticPatterns.PresetType>();
        private EventBinding<HapticStatusIsChangedEvent> _hapticStatusIsChangedEvent;

        public HapticManager(Feedbacks_SO feedbacks_SO, SettingsData settingsData)
        {
            this.feedbacks_SO = feedbacks_SO;
            this.settingsData = settingsData;

            // _hapticStatusIsChangedEvent = new EventBinding<HapticStatusIsChangedEvent>(OnHapticStatusIsChanged);
            // EventBus<HapticStatusIsChangedEvent>.Register(_hapticStatusIsChangedEvent);
        }

        public void Init()
        {
            SetHapticFxDictionary();
        }
        
        public void HapticStatusIsChanged(bool status)
        {
            HapticController.hapticsEnabled = status;
            EventBus<HapticStatusIsChangedEvent>.Raise(new HapticStatusIsChangedEvent(status));
        }

        private void SetHapticFxDictionary()
        {
            foreach (var effect in feedbacks_SO.EffectPresets)
            {
                if (!hapticFxDictionary.ContainsKey(effect.ClipType))
                {
                    #if UNITY_ANDROID
                        hapticFxDictionary.Add(effect.ClipType, effect.androidHapticPreset);
                    #else
                        hapticFxDictionary.Add(effect.ClipType, effect.hapticPresetType);
                    #endif
                }
            }
        }

        public void PlayHaptic(EffectType effectType)
        {
            if (settingsData.GetHapticStatus())
            {
                HapticPatterns.PresetType hapticPreset;
                if (hapticFxDictionary.TryGetValue(effectType, out hapticPreset))
                {
                    HapticPatterns.PlayPreset(hapticPreset);
                }
            }

        }

    }

    [System.Serializable]
    public class HapticFxInfo
    {
        public EffectType clipType;
        public HapticPatterns.PresetType hapticPresetType;
    }
}
