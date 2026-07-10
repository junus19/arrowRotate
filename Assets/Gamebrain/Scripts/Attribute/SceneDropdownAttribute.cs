using UnityEngine;

namespace GameBrain
{
    public class SceneDropdownAttribute : PropertyAttribute
    {
        public readonly bool IncludeDisabledScenes;

        public SceneDropdownAttribute(bool includeDisabledScenes = false)
        {
            IncludeDisabledScenes = includeDisabledScenes;
        }
    }
}
