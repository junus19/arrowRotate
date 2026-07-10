using System.Collections.Generic;
using UnityEngine;

namespace GameBrain.Store
{
    /// <summary>Row of page dots. Rebuilds to a given page count and highlights the selected index.</summary>
    [DisallowMultipleComponent]
    public sealed class PageIndicator : MonoBehaviour
    {
        [SerializeField] private PageIndicatorItem _itemPrefab;

        private readonly List<PageIndicatorItem> _items = new List<PageIndicatorItem>();

        public void Init(int count)
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
                Destroy(transform.GetChild(i).gameObject);
            _items.Clear();

            if (_itemPrefab != null)
            {
                for (int i = 0; i < count; i++)
                    _items.Add(Instantiate(_itemPrefab, transform));
            }

            SetSelectedIndex(0);
            gameObject.SetActive(count > 1); // a single page needs no indicator
        }

        public void SetSelectedIndex(int index)
        {
            for (int i = 0; i < _items.Count; i++)
                _items[i].SetSelected(i == index);
        }
    }
}
