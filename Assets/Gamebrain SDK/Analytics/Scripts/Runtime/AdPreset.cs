using System;
using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.SDK
{
    [Serializable]
    public class AdPreset
    {
        [SerializeField] private AdFrequency _type;
        [Tooltip("Available after the given session number, which means this preset will be activated after the given session number.")]
        [SerializeField, Range(0, 9999)] private int _minimumSessionNumber;
        [SerializeField] private int _frequency;
        [SerializeField] private List<AdPresetCondition> _conditions;
        
        public AdFrequency Type => _type;
        public int MinimumSessionNumber => _minimumSessionNumber;
        public int Frequency => _frequency;
        public List<AdPresetCondition> Conditions => _conditions;

        public bool HasConditions() => _conditions.Count > 0;

        public bool ConditionsMet(int session, int level, float amountOfTimeElapsedFromLastAd)
        {
            foreach (AdPresetCondition condition in _conditions)
            {
                switch(condition.ConditionType)
                {
                    case ConditionType.MinimumLevel:
                        if (level < condition.Value)
                            return false;
                        break;
                    case ConditionType.Time:
                        if (amountOfTimeElapsedFromLastAd < condition.Value)
                            return false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return true;
        }
    }
}
