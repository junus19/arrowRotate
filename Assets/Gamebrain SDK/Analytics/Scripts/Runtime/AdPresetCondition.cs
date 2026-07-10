using System;
using UnityEngine;

namespace GameBrain.SDK
{
    [Serializable]
    public class AdPresetCondition
    {
        [SerializeField] private ConditionType _conditionType;
        [Tooltip("For Condition Type;\n " +
                 "- Level\nMinimum level count should be passed to show an interstitial ad.\n " +
                 "- Time\nMinimum time should be passed to show an interstitial ad after the last interstitial ad shown.\n " +
                 "- Day\nMinimum day should be passed to show an interstitial ad.\n " +
                 "- Session\nMinimum required session index to show an interstitial ad.")]
        [SerializeField] private int _value;

        public ConditionType ConditionType => _conditionType;
        public int Value => _value;
    }
}
