using UnityEngine;

namespace GameBrain.Store
{
    /// <summary>One dot in a <see cref="PageIndicator"/>. Toggles a highlight object for the selected page.</summary>
    [DisallowMultipleComponent]
    public sealed class PageIndicatorItem : MonoBehaviour
    {
        [SerializeField] private GameObject _selected;

        public void SetSelected(bool selected)
        {
            if (_selected != null) _selected.SetActive(selected);
        }
    }
}
