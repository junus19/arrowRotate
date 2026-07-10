using System;
using TMPro;
using UnityEngine;

namespace GameBrain.Store
{
    /// <summary>
    /// Displays a ticking countdown ("2D 23H", "23h 23m", "12m 30s"). Purely a display component — the
    /// offer scheduling/expiry source is supplied externally via <see cref="StartCountdown"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CountdownView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        private DateTime _endUtc;
        private bool _running;

        /// <summary>Raised once when the countdown reaches zero.</summary>
        public event Action Expired;

        public void StartCountdown(TimeSpan duration)
        {
            _endUtc = DateTime.UtcNow + duration;
            _running = duration > TimeSpan.Zero;
            Refresh(_running ? duration : TimeSpan.Zero);
        }

        public void StopCountdown() => _running = false;

        private void Update()
        {
            if (!_running) return;

            TimeSpan remaining = _endUtc - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                _running = false;
                Refresh(TimeSpan.Zero);
                Expired?.Invoke();
                return;
            }

            Refresh(remaining);
        }

        private void Refresh(TimeSpan t)
        {
            if (_text == null) return;

            if (t.TotalDays >= 1)
                _text.text = $"{(int)t.TotalDays}D {t.Hours}H";
            else if (t.TotalHours >= 1)
                _text.text = $"{(int)t.TotalHours}h {t.Minutes}m";
            else
                _text.text = $"{t.Minutes}m {t.Seconds}s";
        }
    }
}
