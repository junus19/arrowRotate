using UnityEngine;

namespace GameBrain.Editor
{
    public class DynamicRangeAttribute : PropertyAttribute
    {
        public readonly string ScriptableObjectTypeName;
        public readonly string ListPropertyName;
    
        public DynamicRangeAttribute(string scriptableObjectTypeName, string listPropertyName)
        {
            ScriptableObjectTypeName = scriptableObjectTypeName;
            ListPropertyName = listPropertyName;
        }
    }
}
