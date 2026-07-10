using UnityEngine;
using System.Collections.Generic;

namespace GameBrain.Casual
{
    public class PageIndicator : MonoBehaviour
    {
        protected List<PageIndicatorItem> _indicatorItems;
        [SerializeField] protected PageIndicatorItem  _pageIndicatorItemPrefab;

        public void Init(int count)
        {
            _indicatorItems = new List<PageIndicatorItem>();
            for (int index = 0; index < count; index++)
            {
                _indicatorItems.Add(Instantiate(_pageIndicatorItemPrefab, transform));
            }
            SetSelectedIndex(0);
        }

        public void SetSelectedIndex(int selectionIndex)
        {
            for (int index = 0; index < _indicatorItems.Count; index++)
            {
                _indicatorItems[index].SetSelected(selectionIndex == index);
            }
        }
    }
}
