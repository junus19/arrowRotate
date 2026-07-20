using System;
using UnityEngine;

namespace ArrowJam
{
    /// <summary>
    /// Single source of truth for snake colors. Every color in the game is read
    /// from here via <see cref="Get"/> — board pieces, exit animation, HUD dots
    /// and the level editor all route through it.
    ///
    /// The asset lives at `Assets/Arrow Jam/Resources/ColorDatabase.asset` so it
    /// loads automatically (no scene reference needed). Edit the per-color values
    /// in the Inspector; calling Get(SnakeColor.Yellow) returns the value you set.
    ///
    /// Create via  Assets ▸ Create ▸ Arrow Jam ▸ Color Database.
    /// </summary>
    [CreateAssetMenu(fileName = "ColorDatabase", menuName = "Arrow Jam/Color Database")]
    public class ArrowJamColorDatabase : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public SnakeColor Color;
            public Color      Value;
        }

        [Tooltip("One entry per SnakeColor. Get() returns the matching Value; " +
                 "missing colors fall back to white.")]
        [SerializeField] private Entry[] _colors = DefaultEntries();

        // ── Lookup ──────────────────────────────────────────────────────────────

        public Color Get(SnakeColor color)
        {
            if (_colors != null)
                for (int i = 0; i < _colors.Length; i++)
                    if (_colors[i].Color == color) return _colors[i].Value;
            return UnityEngine.Color.white;
        }

        // ── Active database (Resources fallback, like ArrowJamThemeData) ──────────

        private static ArrowJamColorDatabase _active;

        /// <summary>
        /// Active database. If not set explicitly, loads
        /// `Resources/ColorDatabase`; if that is missing, a temporary instance
        /// with the built-in defaults is created so colors never break.
        /// </summary>
        public static ArrowJamColorDatabase Active
        {
            get
            {
                if (_active != null) return _active;
                _active = Resources.Load<ArrowJamColorDatabase>("ColorDatabase");
                if (_active == null)
                {
                    Debug.LogWarning("[ArrowJamColorDatabase] ColorDatabase not found in " +
                                     "Resources — using built-in defaults. Create one via " +
                                     "Assets ▸ Create ▸ Arrow Jam ▸ Color Database.");
                    _active = CreateInstance<ArrowJamColorDatabase>();
                }
                return _active;
            }
            set { if (value != null) _active = value; }
        }

        // ── Defaults (match the original hardcoded palette) ───────────────────────

        private static Entry[] DefaultEntries() => new[]
        {
            new Entry { Color = SnakeColor.Yellow, Value = new Color(0.96f, 0.77f, 0.09f) },
            new Entry { Color = SnakeColor.Red,    Value = new Color(0.88f, 0.32f, 0.32f) },
            new Entry { Color = SnakeColor.Blue,   Value = new Color(0.31f, 0.64f, 0.88f) },
            new Entry { Color = SnakeColor.Purple, Value = new Color(0.61f, 0.35f, 0.71f) },
            new Entry { Color = SnakeColor.Green,  Value = new Color(0.33f, 0.78f, 0.38f) },
            new Entry { Color = SnakeColor.Orange, Value = new Color(0.95f, 0.56f, 0.16f) },
            new Entry { Color = SnakeColor.Cyan,   Value = new Color(0.20f, 0.85f, 0.83f) },
            new Entry { Color = SnakeColor.Pink,   Value = new Color(0.96f, 0.56f, 0.76f) },
            new Entry { Color = SnakeColor.Brown,  Value = new Color(0.63f, 0.44f, 0.27f) },
            new Entry { Color = SnakeColor.White,  Value = new Color(0.93f, 0.93f, 0.93f) },
        };
    }
}
