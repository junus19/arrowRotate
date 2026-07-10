using UnityEngine;
using Lofelt.NiceVibrations;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    [CreateAssetMenu(fileName = "Feedbacks_SO", menuName = "Hocus/Create New Feedbacks SO")]
    public class Feedbacks_SO : ScriptableObject
    {
        public List<EffectPreset> EffectPresets;
    }

    [System.Serializable]
    public struct EffectPreset
    {
        public EffectType ClipType;
        // sound fx
        public AudioClip Clip;
        public float Volume;
        public bool HasRandomPitch;
        public float Pitch_Min;
        public float Pitch_Max;
        // haptic
        public HapticPatterns.PresetType hapticPresetType;
        public HapticPatterns.PresetType androidHapticPreset;
    }
}
