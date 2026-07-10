using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GameBrain.Store
{
    /// <summary>
    /// Horizontal paged carousel for the bundle/starter-pack section. Lays out each child as a full
    /// viewport-sized page (so paging is pixel-exact — no leftover sliver), drives a
    /// <see cref="PageIndicator"/>, and snaps to the nearest page when a drag ends.
    ///
    /// The StorePanel instantiates bundle cards into <see cref="Content"/> and then calls
    /// <see cref="Initialize"/>. Layout is (re)applied on the first frame the viewport has a real width
    /// and whenever the rect dimensions change (orientation / canvas scale), so it stays exact.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StoreCarouselView : MonoBehaviour, IEndDragHandler
    {
        [SerializeField] private ScrollRect _scrollRect;
        [SerializeField] private RectTransform _content;
        [SerializeField] private PageIndicator _pageIndicator;
        [Tooltip("Higher = snappier page settle.")]
        [SerializeField] private float _snapSpeed = 12f;
        [Tooltip("Horizontal margin on each side of a card inside its page. Cards stay centered when " +
                 "snapped; the visible gap between two cards equals 2 × this value.")]
        [SerializeField] private float _sidePadding = 30f;

        private int _pageCount;
        private float _targetNormalizedX;
        private bool _snapping;
        private bool _pendingLayout;

        public Transform Content => _content;

        public void Initialize(int pageCount)
        {
            _pageCount = Mathf.Max(0, pageCount);
            _snapping = false;
            _pendingLayout = true;
            if (_pageIndicator != null) _pageIndicator.Init(_pageCount);
        }

        private void OnEnable()
        {
            if (_scrollRect != null) _scrollRect.onValueChanged.AddListener(OnScrollChanged);
        }

        private void OnDisable()
        {
            if (_scrollRect != null) _scrollRect.onValueChanged.RemoveListener(OnScrollChanged);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (_pageCount > 0) _pendingLayout = true;
        }

        private void Update()
        {
            if (_pendingLayout)
            {
                float viewportWidth = ViewportWidth();
                if (_content != null && viewportWidth > 1f)
                {
                    LayoutPages(viewportWidth, ViewportHeight());
                    _pendingLayout = false;
                    _snapping = false;
                    if (_scrollRect != null) _scrollRect.horizontalNormalizedPosition = 0f;
                    if (_pageIndicator != null) _pageIndicator.SetSelectedIndex(0);
                }
            }

            if (_snapping && _scrollRect != null)
            {
                float next = Mathf.Lerp(_scrollRect.horizontalNormalizedPosition, _targetNormalizedX,
                    Time.unscaledDeltaTime * _snapSpeed);
                if (Mathf.Abs(next - _targetNormalizedX) < 0.0005f)
                {
                    next = _targetNormalizedX;
                    _snapping = false;
                }
                _scrollRect.horizontalNormalizedPosition = next;
            }
        }

        private void OnScrollChanged(Vector2 _)
        {
            if (_pageIndicator != null) _pageIndicator.SetSelectedIndex(CurrentPage());
        }

        private int CurrentPage()
        {
            if (_pageCount <= 1 || _scrollRect == null) return 0;
            float x = Mathf.Clamp01(_scrollRect.horizontalNormalizedPosition);
            return Mathf.Clamp(Mathf.RoundToInt(x * (_pageCount - 1)), 0, _pageCount - 1);
        }

        public void OnEndDrag(PointerEventData eventData) => SnapTo(CurrentPage());

        public void SnapTo(int page)
        {
            if (_pageCount <= 1 || _scrollRect == null) return;
            _targetNormalizedX = (float)page / (_pageCount - 1);
            _snapping = true;
        }

        private float ViewportWidth()
        {
            if (_scrollRect != null && _scrollRect.viewport != null) return _scrollRect.viewport.rect.width;
            return ((RectTransform)transform).rect.width;
        }

        private float ViewportHeight()
        {
            if (_scrollRect != null && _scrollRect.viewport != null) return _scrollRect.viewport.rect.height;
            return ((RectTransform)transform).rect.height;
        }

        /// <summary>Each child becomes exactly one viewport-sized page; content width = pages * viewport width.</summary>
        private void LayoutPages(float pageWidth, float pageHeight)
        {
            int n = _content.childCount;

            _content.anchorMin = new Vector2(0f, 0f);
            _content.anchorMax = new Vector2(0f, 1f);
            _content.pivot = new Vector2(0f, 0.5f);
            _content.sizeDelta = new Vector2(n * pageWidth, 0f);

            float cardWidth = Mathf.Max(0f, pageWidth - 2f * _sidePadding);

            for (int i = 0; i < n; i++)
            {
                RectTransform card = _content.GetChild(i) as RectTransform;
                if (card == null) continue;

                // The page slot is exactly one viewport wide (so paging snaps perfectly); the card sits
                // inset by _sidePadding on each side — equal margins keep it centered when snapped, and the
                // gap between two adjacent cards is 2 × _sidePadding.
                card.anchorMin = new Vector2(0f, 0.5f);
                card.anchorMax = new Vector2(0f, 0.5f);
                card.pivot = new Vector2(0f, 0.5f);
                card.sizeDelta = new Vector2(cardWidth, pageHeight);
                card.anchoredPosition = new Vector2(i * pageWidth + _sidePadding, 0f);
            }
        }
    }
}
