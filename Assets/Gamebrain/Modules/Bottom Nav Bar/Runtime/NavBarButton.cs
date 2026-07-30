using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameBrain.Casual
{
    /// <summary>
    /// One button of the <see cref="NavBar"/> — visuals AND behaviour in a single component.
    ///
    /// Everything a button can be is inspector data:
    ///  - <see cref="Mode.SelectOnly"/>  — plain selection, opens nothing (Home).
    ///  - <see cref="Mode.OpenPanel"/>   — activates a <see cref="UIPanel"/> while selected.
    ///  - <see cref="Mode.Placeholder"/> — renders locked and only shows a message ("Coming soon!").
    /// Plus an optional level gate ("unlocks at Level 20").
    ///
    /// Tapping a locked or placeholder button floats its message up above the button and fades it out — the
    /// feedback is LOCAL: no events, no host wiring, nothing to subscribe to.
    ///
    /// A button is addressed by its <see cref="Index"/> — its position in the row — so there is no id to keep
    /// in sync. Selection is driven by the bar; the button never decides it alone.
    /// To hide a button entirely (feature not shipped) just deactivate its GameObject — it then also leaves the
    /// layout group and the remaining buttons re-centre.
    /// </summary>
    [DisallowMultipleComponent]
    public class NavBarButton : MonoBehaviour
    {
        public enum Mode
        {
            /// <summary>Selects itself and nothing more (Home).</summary>
            SelectOnly = 0,

            /// <summary>Activates <see cref="Panel"/> while selected.</summary>
            OpenPanel = 1,

            /// <summary>Always renders locked; a tap only shows a message and never moves the selection.</summary>
            Placeholder = 2,
        }

        [Header("Action")]
        [SerializeField] private Mode _mode = Mode.SelectOnly;

        [ShowIf("_mode", (int)Mode.OpenPanel)]
        [Tooltip("Panel activated while this button is selected.")]
        [SerializeField] private UIPanel _panel;

        [ShowIf("_mode", (int)Mode.OpenPanel)]
        [Tooltip("Deactivate the panel when the selection leaves this button.")]
        [SerializeField] private bool _closePanelOnDeselect = true;

        [ShowIf("_mode", (int)Mode.Placeholder)]
        [Tooltip("Message floated above the button when this placeholder is tapped.")]
        [SerializeField] private string _placeholderFeedback = "Coming soon!";

        [Header("Level lock (optional)")]
        [Tooltip("Locked until the player's visual level reaches Unlock At Level. 0 = never locked.")]
        [SerializeField] private int _unlockAtLevel;

        [Tooltip("GameData asset — the source of the player's visual level.")]
        [SerializeField] private GameData _gameData;

        [Tooltip("Label while locked; {0} = unlock level.")]
        [SerializeField] private string _lockLabelFormat = "Level {0}";

        [Tooltip("Message floated above the button when it is tapped while locked; {0} = unlock level.")]
        [SerializeField] private string _lockedFeedbackFormat = "Unlocks at Level {0}!";

        [Header("Tap feedback")]
        [Tooltip("Text that floats up and fades out above the button. Kept inactive until needed.")]
        [SerializeField] private TextMeshProUGUI _feedbackText;

        [Tooltip("How far the message travels upwards, in canvas units.")]
        [SerializeField] private float _feedbackRise = 70f;

        [Tooltip("How long the whole float-and-fade takes, in seconds.")]
        [SerializeField] private float _feedbackDuration = 1f;

        [Header("Selected emphasis")]
        [Tooltip("Layout element on THIS button. Its preferred width carries the growth, so the WHOLE button gets " +
                 "wider and the neighbours make room instead of being overlapped.")]
        [SerializeField] private LayoutElement _layoutElement;

        [Tooltip("Extra transform scaled on top of the rect growth. Leave empty when the icon is driven by the " +
                 "two fields below — otherwise it would grow twice.")]
        [SerializeField] private Transform _visual;

        [Tooltip("Icon size while selected, in canvas units (square). The authored size is the resting one. " +
                 "0 leaves the icon size alone.")]
        [SerializeField] private float _iconSelectedSize = 160f;

        [Tooltip("Extra upward offset for the icon while selected, in canvas units — this is what opens the gap " +
                 "to the label. The button's own height growth already lifts the icon; this is on top of it.")]
        [SerializeField] private float _iconRise = 16f;

        [Tooltip("Size factor while selected.")]
        [SerializeField] private float _selectedScale = 1.1f;

        [Tooltip("Size factor while not selected — below 1 makes the other buttons visibly smaller.")]
        [SerializeField] private float _restScale = 0.9f;

        [Tooltip("Width at factor 1. 0 = whatever the layout group hands the button at startup.")]
        [SerializeField] private float _baseWidth;

        [Tooltip("Extra height for the selected button, in canvas units. Grows UPWARD — the row's Child " +
                 "Alignment has to be one of the Lower ones. 0 disables it.")]
        [SerializeField] private float _selectedRise = 40f;

        [Tooltip("Background tinted by selection — the ONLY thing the component colours. Optional.")]
        [SerializeField] private Graphic _background;

        [Tooltip("Background colour while selected.")]
        [SerializeField] private Color _selectedColor = new Color(0.753f, 0.753f, 0.753f);

        [Tooltip("Background colour while not selected.")]
        [SerializeField] private Color _unselectedColor = Color.white;

        [SerializeField] private float _emphasisDuration = 0.25f;
        [SerializeField] private Ease _emphasisEase = Ease.OutBack;

        [Header("State roots")]
        [Tooltip("Everything that makes up the UNLOCKED look. Active while the button is usable.")]
        [SerializeField] private GameObject _unlockedState;

        [Tooltip("Everything that makes up the LOCKED look (lock icon, requirement text, its own background…). " +
                 "Active while the button is locked. The two are mutually exclusive.")]
        [SerializeField] private GameObject _lockedState;

        [Header("Visuals")]
        [SerializeField] private Button _button;

        [Tooltip("Icon of the UNLOCKED state — the one the selected emphasis grows.")]
        [SerializeField] private Image _icon;

        [Tooltip("Label of the UNLOCKED state; shown only while this button is selected.")]
        [SerializeField] private TextMeshProUGUI _label;

        [Tooltip("Label inside the locked state; receives the Lock Label Format text (e.g. \"Level 20\").")]
        [SerializeField] private TextMeshProUGUI _lockedLabel;

        [Tooltip("Notification dot, shown while the badge count is > 0.")]
        [SerializeField] private GameObject _bullet;

        [Tooltip("Optional counter inside the dot. Without it the dot is a plain bullet.")]
        [SerializeField] private TextMeshProUGUI _badgeText;

        // The message pops in quickly and then spends the rest of its life fading out.
        private const float FeedbackFadeIn = 0.12f;

        private NavBar _bar;
        private RectTransform _rect;
        private RectTransform _iconRect;
        private Vector2 _iconRestSize;
        private float _iconRestPosY;
        private Vector3 _visualBaseScale = Vector3.one;
        private float _resolvedBaseWidth;
        private float _baseHeight;
        private float _emphasisT;
        private Tween _emphasisTween;
        private RectTransform _feedbackRect;
        private Vector2 _feedbackHome;
        private Sequence _feedbackTween;
        private string _defaultLabel;
        private bool _defaultLabelCaptured;
        private bool _bound;
        private bool _selected;
        private bool _selectionKnown;
        private int _badge;

        /// <summary>Position in the row — this is the button's address (NavBar.Select(index)).</summary>
        public int Index => transform.GetSiblingIndex();

        public bool IsSelected => _selected;
        public Mode ButtonMode => _mode;

        /// <summary>Assigned panel (OpenPanel mode). Settable for late/remote binding.</summary>
        public UIPanel Panel
        {
            get => _mode == Mode.OpenPanel ? _panel : null;
            set { _panel = value; Refresh(); }
        }

        public bool IsLocked => _mode == Mode.Placeholder || IsLevelLocked;

        /// <summary>Can this button hold the bar's selection right now?</summary>
        public bool IsSelectable => gameObject.activeSelf && !IsLocked;

        private bool IsLevelLocked =>
            _unlockAtLevel > 0 && _gameData != null && _gameData.GetVisualLevelIndex() < _unlockAtLevel;

        /// <summary>Notification badge; 0 hides it.</summary>
        public int Badge
        {
            get => _badge;
            set
            {
                if (_badge == value) return;
                _badge = value;
                Refresh();
            }
        }

        // ---- wiring ----------------------------------------------------------

        // Called by the bar (not Awake): a deactivated button has no Awake, yet it must be fully wired the
        // moment it is switched on again.
        internal void Bind(NavBar bar)
        {
            _bar = bar;
            if (_bound) return;
            _bound = true;

            CaptureDefaultLabel();
            if (_button != null) _button.onClick.AddListener(OnClicked);

            // The authored scale is the baseline the multipliers apply to, so art that ships at a non-1 scale
            // keeps working.
            if (_visual != null) _visualBaseScale = _visual.localScale;

            _rect = (RectTransform)transform;
            _baseHeight = _rect.sizeDelta.y;

            // The authored icon rect IS the resting state; the selected state is derived from it.
            if (_icon != null)
            {
                _iconRect = _icon.rectTransform;
                _iconRestSize = _iconRect.sizeDelta;
                _iconRestPosY = _iconRect.anchoredPosition.y;
            }

            if (_layoutElement != null)
            {
                // Explicit width wins; otherwise take whatever the layout group has already handed this button
                // (resolution-independent, since the group recomputes it per device).
                _resolvedBaseWidth = _baseWidth > 0f ? _baseWidth
                    : _layoutElement.preferredWidth > 0f ? _layoutElement.preferredWidth
                    : _rect.rect.width;
            }

            ApplyEmphasis(false);

            if (_feedbackText != null)
            {
                // The resting position is captured ONCE, before any tween can move it, so repeated taps always
                // start from the same spot.
                _feedbackRect = _feedbackText.rectTransform;
                _feedbackHome = _feedbackRect.anchoredPosition;
                _feedbackText.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            ResetFeedback();
            ResetEmphasis();
        }

        private void OnDestroy()
        {
            ResetFeedback();
            _emphasisTween?.Kill();
            if (_button != null) _button.onClick.RemoveListener(OnClicked);
        }

        // The bar can drive this button BEFORE its Awake ran, so the default label is captured lazily and only
        // while the text is still non-empty — otherwise an early lock would blank it and the blank would stick.
        private void CaptureDefaultLabel()
        {
            if (_defaultLabelCaptured) return;
            if (_label != null && !string.IsNullOrEmpty(_label.text))
            {
                _defaultLabel = _label.text;
                _defaultLabelCaptured = true;
            }
        }

        // ---- input -----------------------------------------------------------

        // Locked buttons stay CLICKABLE by design: the tap becomes a message instead of navigation.
        private void OnClicked()
        {
            if (IsLocked)
            {
                ShowFeedback(LockedMessage);
                return;
            }

            if (_bar != null) _bar.Select(this);
        }

        private string LockedMessage
        {
            get
            {
                if (_mode == Mode.Placeholder) return _placeholderFeedback;
                return string.IsNullOrEmpty(_lockedFeedbackFormat)
                    ? null
                    : string.Format(_lockedFeedbackFormat, _unlockAtLevel);
            }
        }

        // ---- selected emphasis -----------------------------------------------

        /// <summary>
        /// The selected button grows — WIDTH via the layout element (so the row makes room instead of letting it
        /// overlap), HEIGHT upwards via its own rect, and the fixed-size visual (icon) by scale on top. All three
        /// are driven from ONE tween of a 0..1 weight, so they can never drift apart, not even with an
        /// overshooting ease.
        /// </summary>
        private void ApplyEmphasis(bool animated)
        {
            if (!HasEmphasis) return;

            _emphasisTween?.Kill();
            _emphasisTween = null;

            // A locked button never grows, even if something selected it programmatically.
            float target = _selected && !IsLocked ? 1f : 0f;
            if (!animated || _emphasisDuration <= 0f)
            {
                SetEmphasis(target);
                return;
            }

            _emphasisTween = DOTween.To(() => _emphasisT, SetEmphasis, target, _emphasisDuration)
                .SetEase(_emphasisEase);
        }

        private bool HasEmphasis =>
            _layoutElement != null || _visual != null || _background != null ||
            !Mathf.Approximately(_selectedRise, 0f) || DrivesIcon;

        private bool DrivesIcon =>
            _iconRect != null && (_iconSelectedSize > 0f || !Mathf.Approximately(_iconRise, 0f));

        // weight: 0 = not selected, 1 = selected (an overshooting ease pushes it past 1 on purpose)
        private void SetEmphasis(float weight)
        {
            _emphasisT = weight;
            float factor = Mathf.LerpUnclamped(_restScale, _selectedScale, weight);

            if (_visual != null) _visual.localScale = _visualBaseScale * factor;

            // Same weight, so the tint lands exactly with the growth. Color.Lerp clamps, which keeps an
            // overshooting ease from pushing the colour past either end.
            if (_background != null) _background.color = Color.Lerp(_unselectedColor, _selectedColor, weight);

            // The icon is driven through its RECT, not a scale: the selected size is an exact number (no blurry
            // upscale) and the same weight lifts it, which is what opens the gap to the label.
            if (DrivesIcon)
            {
                if (_iconSelectedSize > 0f)
                    _iconRect.sizeDelta = new Vector2(
                        Mathf.LerpUnclamped(_iconRestSize.x, _iconSelectedSize, weight),
                        Mathf.LerpUnclamped(_iconRestSize.y, _iconSelectedSize, weight));

                Vector2 iconPos = _iconRect.anchoredPosition;
                iconPos.y = _iconRestPosY + _iconRise * weight;
                _iconRect.anchoredPosition = iconPos;
            }

            // Setting preferredWidth marks the layout dirty; the group redistributes at the end of the frame.
            if (_layoutElement != null) _layoutElement.preferredWidth = _resolvedBaseWidth * factor;

            if (Mathf.Approximately(_selectedRise, 0f) || _rect == null) return;

            Vector2 size = _rect.sizeDelta;
            size.y = _baseHeight + _selectedRise * weight;
            _rect.sizeDelta = size;

            // A child's own size change does not dirty its layout group, and without a rebuild the row would not
            // re-align the taller button — which is what makes it grow UPWARD instead of downward.
            if (_rect.parent is RectTransform row) LayoutRebuilder.MarkLayoutForRebuild(row);
        }

        // Deactivating mid-tween must not freeze the button at a half-grown size.
        private void ResetEmphasis()
        {
            if (!HasEmphasis) return;
            _emphasisTween?.Kill();
            _emphasisTween = null;
            SetEmphasis(_selected && !IsLocked ? 1f : 0f);
        }

        // ---- feedback --------------------------------------------------------

        /// <summary>
        /// Floats <paramref name="message"/> up above the button and fades it out. Local by design: no events,
        /// no host handler, nothing to subscribe to. Tapping again restarts the animation from the top.
        /// </summary>
        public void ShowFeedback(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (_feedbackText == null)
            {
                Debug.LogWarning($"[NavBar] Button at index {Index} has no feedback text assigned — " +
                                 $"message dropped: {message}", this);
                return;
            }

            _feedbackTween?.Kill();

            _feedbackText.text = message;
            _feedbackRect.anchoredPosition = _feedbackHome;
            _feedbackText.alpha = 0f;
            _feedbackText.gameObject.SetActive(true);

            float duration = Mathf.Max(FeedbackFadeIn * 2f, _feedbackDuration);
            _feedbackTween = DOTween.Sequence()
                .Append(_feedbackText.DOFade(1f, FeedbackFadeIn))
                .Join(_feedbackRect.DOAnchorPosY(_feedbackHome.y + _feedbackRise, duration).SetEase(Ease.OutCubic))
                .Append(_feedbackText.DOFade(0f, duration - FeedbackFadeIn))
                .OnComplete(HideFeedback);
        }

        private void HideFeedback()
        {
            if (_feedbackText != null) _feedbackText.gameObject.SetActive(false);
        }

        // Deactivating mid-flight must not leave a half-faded message hanging at a half-travelled position.
        private void ResetFeedback()
        {
            _feedbackTween?.Kill();
            _feedbackTween = null;

            if (_feedbackRect != null) _feedbackRect.anchoredPosition = _feedbackHome;
            HideFeedback();
        }

        // ---- state -----------------------------------------------------------

        internal void ApplySelection(bool selected)
        {
            bool first = !_selectionKnown;
            bool changed = first || _selected != selected;

            _selected = selected;
            _selectionKnown = true;

            if (changed && _mode == Mode.OpenPanel && _panel != null)
            {
                if (selected) _panel.SetActive(true);
                else if (!first && _closePanelOnDeselect && _panel.gameObject.activeSelf)
                    _panel.SetActive(false); // never "close" before the first selection
            }

            if (changed) ApplyEmphasis(!first); // the very first application snaps, later ones animate
            Refresh();
        }

        /// <summary>Repaints from the current state (lock, selection, badge). Safe to call any time.</summary>
        public void Refresh()
        {
            CaptureDefaultLabel();
            bool locked = IsLocked;

            // Locked and unlocked are two separate layouts; the component only decides which one is live and
            // never tints or swaps sprites, so the art keeps full control of both looks.
            if (_lockedState != null && _lockedState.activeSelf != locked) _lockedState.SetActive(locked);
            if (_unlockedState != null && _unlockedState.activeSelf == locked) _unlockedState.SetActive(!locked);

            // The requirement text belongs to a LEVEL gate only. A placeholder ("Coming soon") is locked too but
            // has no level, so its locked label is switched off instead of showing a stale number.
            if (_lockedLabel != null)
            {
                bool showRequirement = IsLevelLocked && !string.IsNullOrEmpty(_lockLabelFormat);
                if (showRequirement) _lockedLabel.text = string.Format(_lockLabelFormat, _unlockAtLevel);
                if (_lockedLabel.gameObject.activeSelf != showRequirement)
                    _lockedLabel.gameObject.SetActive(showRequirement);
            }

            if (_label != null)
            {
                if (_defaultLabelCaptured) _label.text = _defaultLabel; // never write a blank default

                // Only the selected button is labelled. A level-locked button shows its requirement through the
                // locked state instead — unless no locked state is wired, in which case this label carries it.
                bool showLabel = _selected || (IsLevelLocked && _lockedState == null);
                if (_label.gameObject.activeSelf != showLabel) _label.gameObject.SetActive(showLabel);
            }

            bool badgeVisible = _badge > 0 && !locked;
            if (_bullet != null) _bullet.SetActive(badgeVisible);
            if (_badgeText != null && badgeVisible) _badgeText.text = _badge.ToString();
        }
    }
}
