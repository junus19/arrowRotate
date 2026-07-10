using UnityEngine;

namespace GameBrain.Editor
{
    public class DropdownAttribute : PropertyAttribute
    {
        public readonly string[] Options;

        public DropdownAttribute(params string[] options)
        {
            Options = options;
        }
    }
}
