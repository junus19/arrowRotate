using UnityEngine;

namespace GameBrain
{
    public class HideIfAttribute : PropertyAttribute
    {
        public string ConditionalPropertyName { get; }
        public object CompareValue { get; }

        public HideIfAttribute(string conditionalPropertyName, object compareValue)
        {
            this.ConditionalPropertyName = conditionalPropertyName;
            this.CompareValue = compareValue;
        }
    }
}