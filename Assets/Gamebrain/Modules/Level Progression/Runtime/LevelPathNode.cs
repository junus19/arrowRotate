using DG.Tweening;
using TMPro;
using UnityEngine;

namespace GameBrain.Casual
{
    /// <summary>
    /// One stop on the <see cref="LevelPath"/> — a single component owning everything a node shows.
    ///
    /// Like the nav bar's button, the two looks are two mutually exclusive containers, so the current and the
    /// upcoming node are art-directed independently (different circle, ring, size…) and this component only
    /// decides which one is live. It never tints or scales anything.
    /// </summary>
    [DisallowMultipleComponent]
    public class LevelPathNode : MonoBehaviour
    {
        [Header("State roots")]
        [Tooltip("The look of the level the player is on right now (the ringed node in the design).")]
        [SerializeField] private GameObject _currentState;

        [Tooltip("The look of a level that is still ahead.")]
        [SerializeField] private GameObject _upcomingState;

        [Header("Content")]
        [Tooltip("Level number of the CURRENT state. Optional if that state has no number.")]
        [SerializeField] private TextMeshProUGUI _currentNumber;

        [Tooltip("Level number of the UPCOMING state.")]
        [SerializeField] private TextMeshProUGUI _upcomingNumber;

        [Tooltip("Shown when this level is flagged Hard in the game config (the 'Hard' pill in the design).")]
        [SerializeField] private GameObject _hardTag;

        [Header("Current-node pulse")]
        [Tooltip("Transform that breathes while this node is the current level — usually the ring. Optional.")]
        [SerializeField] private Transform _ring;

        [Tooltip("Peak scale, as a multiplier over the authored scale.")]
        [SerializeField] private float _pulseScale = 1.08f;

        [Tooltip("Seconds for one half of the pulse (out, then back in).")]
        [SerializeField] private float _pulseDuration = 0.8f;

        [SerializeField] private Ease _pulseEase = Ease.InOutSine;

        private Vector3 _ringBaseScale = Vector3.one;
        private bool _ringBaseCaptured;
        private Tween _pulseTween;

        /// <summary>The level number this node currently displays (1-based, as the player sees it).</summary>
        public int Level { get; private set; }

        public bool IsCurrent { get; private set; }

        /// <summary>
        /// Paints the node. Called by <see cref="LevelPath"/> for every node on every refresh, so it must be
        /// idempotent and cheap.
        /// </summary>
        public void Setup(int level, bool isCurrent, bool isHard)
        {
            Level = level;
            IsCurrent = isCurrent;

            if (_currentState != null && _currentState.activeSelf != isCurrent)
                _currentState.SetActive(isCurrent);
            if (_upcomingState != null && _upcomingState.activeSelf == isCurrent)
                _upcomingState.SetActive(!isCurrent);

            string text = level.ToString();
            if (_currentNumber != null) _currentNumber.text = text;
            if (_upcomingNumber != null) _upcomingNumber.text = text;

            if (_hardTag != null && _hardTag.activeSelf != isHard) _hardTag.SetActive(isHard);

            SetPulsing(isCurrent);
        }

        // ---- pulse -----------------------------------------------------------

        private void OnDisable() => StopPulse();

        private void OnDestroy() => StopPulse();

        /// <summary>
        /// Breathes the ring while this node is the current level. Setup runs on every refresh, so an already
        /// running pulse is left alone instead of being restarted mid-cycle.
        /// </summary>
        private void SetPulsing(bool on)
        {
            if (_ring == null) return;

            if (!_ringBaseCaptured)
            {
                // Captured before any tween can touch it, so art shipped at a non-1 scale keeps working.
                _ringBaseScale = _ring.localScale;
                _ringBaseCaptured = true;
            }

            // Never tween outside play mode: the editor has no player loop driving DOTween, and a half-applied
            // scale would be saved into the scene or the prefab.
            if (!on || !Application.isPlaying)
            {
                StopPulse();
                return;
            }

            if (_pulseTween != null && _pulseTween.IsActive()) return;

            _ring.localScale = _ringBaseScale;
            _pulseTween = _ring.DOScale(_ringBaseScale * _pulseScale, Mathf.Max(0.01f, _pulseDuration))
                .SetEase(_pulseEase)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void StopPulse()
        {
            _pulseTween?.Kill();
            _pulseTween = null;
            if (_ring != null && _ringBaseCaptured) _ring.localScale = _ringBaseScale;
        }
    }
}
