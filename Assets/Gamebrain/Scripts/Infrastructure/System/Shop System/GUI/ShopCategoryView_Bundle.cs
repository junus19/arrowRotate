using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    public class ShopCategoryView_Bundle : ShopCategoryView
    {
        [SerializeField] protected ScrollRect _scrollRect;
        [SerializeField] protected PageIndicator _pageIndicator;

        public override void Init(ShopCategoryData data)
        {
            base.Init(data);
            
            _pageIndicator.Init(_contentContainer.childCount);
        }

        protected virtual void OnEnable()
        {
            _scrollRect.onValueChanged.AddListener(OnScrollRectValueChanged);
        }

        protected virtual void OnDisable()
        {
            _scrollRect.onValueChanged.RemoveListener(OnScrollRectValueChanged);
        }

        private void OnScrollRectValueChanged(Vector2 scrollValue)
        {
            float pageWidth = 1f / _contentContainer.childCount;
            int currentPageIndex = (int)(scrollValue.x / pageWidth);
            currentPageIndex = Mathf.Clamp(currentPageIndex, 0, _contentContainer.childCount - 1);
            _pageIndicator.SetSelectedIndex(currentPageIndex);
        }
    }
}
