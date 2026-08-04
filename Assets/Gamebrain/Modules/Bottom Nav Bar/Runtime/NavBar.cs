using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameBrain.Casual
{
    /// <summary>
    /// The bottom navigation bar. It owns exactly one thing: which button is selected.
    ///
    /// Its buttons are the <see cref="NavBarButton"/> children of <see cref="_buttonsRoot"/> (inactive ones
    /// included), so the button count is data rather than code — duplicate a button GameObject and it joins the
    /// bar. What each button DOES is that button's own inspector setting. Buttons are addressed by their child
    /// index (<see cref="Select(int)"/>); the fall-back is the explicit <see cref="_defaultButton"/> reference.
    ///
    /// Visibility is NOT this component's business: the bar lives inside the main-menu panel, so it appears and
    /// disappears with its parent. Every re-activation runs <see cref="OnEnable"/>, which repaints the buttons
    /// and (optionally) returns the selection to the default one.
    /// </summary>
    [DisallowMultipleComponent]
    public class NavBar : MonoBehaviour
    {
        [Header("Buttons")]
        [Tooltip("Container whose NavBarButton children make up the bar. Defaults to this GameObject.")]
        [SerializeField] private Transform _buttonsRoot;

        [Tooltip("Buttons of this bar. Leave empty to collect every button under the root on Awake.")]
        [SerializeField] private List<NavBarButton> _buttons = new List<NavBarButton>();

        [Tooltip("Button the bar falls back to (Home). Empty or unusable → the first usable button.")]
        [SerializeField] private NavBarButton _defaultButton;

        [Tooltip("Return to the default button whenever the bar is (re)activated — i.e. on every menu visit.")]
        [SerializeField] private bool _selectDefaultOnEnable = true;

        private NavBarButton _current;
        private bool _watchingPanel;
        private bool _initialized;
        private bool _selecting;

        /// <summary>Fires after the selection actually changed.</summary>
        public event Action<NavBarButton> SelectionChanged;

        public List<NavBarButton> Buttons
        {
            get
            {
                Initialize();
                return _buttons;
            }
        }

        public NavBarButton Current => _current;

        private void Awake() => Initialize();

        private void OnEnable()
        {
            Refresh();
            if (_selectDefaultOnEnable) SelectDefault();
        }

        private void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            if (_buttonsRoot == null) _buttonsRoot = transform;

            if (_buttons == null) _buttons = new List<NavBarButton>();
            _buttons.RemoveAll(button => button == null);
            if (_buttons.Count == 0)
                _buttons.AddRange(_buttonsRoot.GetComponentsInChildren<NavBarButton>(true));

            if (_defaultButton != null && !_buttons.Contains(_defaultButton))
            {
                Debug.LogWarning($"[NavBar] Default button '{_defaultButton.name}' is not part of this bar — " +
                                 "falling back to the first usable one.", this);
                _defaultButton = null;
            }

            foreach (NavBarButton button in _buttons)
                button.Bind(this);
        }

        // A panel closed from the outside (its own X button, a back gesture) must not leave its button selected.
        private void Update()
        {
            if (!_watchingPanel || _current == null) return;

            UIPanel panel = _current.Panel;
            if (panel == null || panel.gameObject.activeSelf) return;

            _watchingPanel = false;
            SelectDefault();
        }

        // ---- selection --------------------------------------------------------

        /// <summary>Selects by child index. False if there is no such button, or it is locked/deactivated.</summary>
        public bool Select(int index)
        {
            NavBarButton button = GetButton(index);
            if (button == null || !button.IsSelectable) return false;
            Select(button);
            return true;
        }

        public bool TrySelect(int index)
        {
            NavBarButton button = GetButton(index);
            if (!button.IsUnlocked()) return false;
            Select(index);
            return true;
        }

        public void Select(NavBarButton button)
        {
            Initialize();
            if (button == null || _selecting || _current == button) return;
            if (!_buttons.Contains(button))
            {
                Debug.LogWarning($"[NavBar] Button at index {button.Index} is not part of this bar.", button);
                return;
            }
            if (!button.IsSelectable) return;

            _selecting = true;
            try
            {
                // Deselect first, so the outgoing panel closes BEFORE the incoming one opens.
                foreach (NavBarButton other in _buttons)
                    if (other != button)
                        other.ApplySelection(false);

                _current = button;
                button.ApplySelection(true);
                _watchingPanel = button.Panel != null;
            }
            finally
            {
                _selecting = false;
            }

            SelectionChanged?.Invoke(_current);
        }

        /// <summary>Selects the default button, falling back to the first usable one.</summary>
        public void SelectDefault()
        {
            Initialize();
            NavBarButton target = _defaultButton != null && _defaultButton.IsSelectable ? _defaultButton : null;

            if (target == null)
                foreach (NavBarButton button in _buttons)
                    if (button.IsSelectable) { target = button; break; }

            if (target != null) Select(target);
        }

        /// <summary>The button sitting at <paramref name="index"/> in the row, or null.</summary>
        public NavBarButton GetButton(int index)
        {
            Initialize();
            foreach (NavBarButton button in _buttons)
                if (button.Index == index) return button;
            return null;
        }

        // ---- refresh ----------------------------------------------------------

        /// <summary>Repaints every button (level locks, badges).</summary>
        public void Refresh()
        {
            Initialize();
            foreach (NavBarButton button in _buttons)
                button.Refresh();

            // A button that locked itself while selected must hand the selection back.
            if (_current != null && !_current.IsSelectable) SelectDefault();
        }

#if UNITY_EDITOR
        [ContextMenu("Collect Buttons From Children")]
        private void CollectButtons()
        {
            Transform root = _buttonsRoot != null ? _buttonsRoot : transform;
            _buttons = new List<NavBarButton>(root.GetComponentsInChildren<NavBarButton>(true));
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
