using UnityEngine;

namespace GameBrain
{
    public class HideIfAnyAttribute : PropertyAttribute
    {
        public string ConditionalPropertyName { get; }
        public object[] CompareValue { get; }

        public HideIfAnyAttribute(string conditionalPropertyName, object[] compareValues)
        {
            this.ConditionalPropertyName = conditionalPropertyName;
            this.CompareValue = compareValues;
        }
    }
}
