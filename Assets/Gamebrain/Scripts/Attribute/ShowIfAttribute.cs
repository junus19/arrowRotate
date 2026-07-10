using UnityEngine;

namespace GameBrain
{
    public class ShowIfAttribute : PropertyAttribute
    {
        public string ConditionalPropertyName { get; }
        public object CompareValue { get; }
        public string Header { get; }

        public ShowIfAttribute(string conditionalPropertyName, object compareValue, string header = "")
        {
            ConditionalPropertyName = conditionalPropertyName;
            CompareValue = compareValue;
            Header = header;
        }
    }
}
