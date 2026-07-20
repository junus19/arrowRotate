using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Board görsel teması — arrowJam ThemeData deseninin Hexa uyarlaması.
    /// Ok PALET renkleri temadan bağımsızdır (bkz. HexaColorDatabase); tema yalnız zemin,
    /// boş hücre, buz ve editör önizleme renklerini taşır.
    ///
    /// Not: Hexa runtime board'u (Flat2D) grid arka planı çizmez — yalnız dolu taşlar + segment.
    /// Bu yüzden tema arrowJam'e göre daha küçüktür (grid/tile shadow alanları yok).
    ///
    /// Asset'ler `Assets/_Arrow Rotate/Resources/Themes/` altında yaşar.
    /// Aktif tema atanmazsa Resources'tan Theme_Default yüklenir.
    /// Oluştur: Assets ▸ Create ▸ Arrow Rotate ▸ Theme.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme_New", menuName = "Arrow Rotate/Theme")]
    public class HexaThemeData : ScriptableObject
    {
        [Header("Zemin")]
        [Tooltip("Kamera arka plan rengi (2D mod). 3D mod kamerayı CameraPrefab'dan okur.")]
        public Color CameraBackground = Hex("#5b6285");

        [Header("Boş Hücre (editör önizleme + olası runtime grid)")]
        public Color EmptyHex = new Color(0.16f, 0.16f, 0.24f);
        public Color EmptyDot = new Color(0.40f, 0.40f, 0.60f);
        public Color GridLine = new Color(0.22f, 0.22f, 0.33f);

        [Header("Segment Gölgesi")]
        [Tooltip("Beyaz okların altındaki gri gölge/outline rengi (Shapes2D).")]
        public Color SegmentShadow = new Color(0.35f, 0.35f, 0.42f, 0.65f);

        [Header("Buz (SKILL §5)")]
        public Color IceFill = new Color(198f / 255f, 230f / 255f, 1f, 0.62f);
        public Color IceEdge = new Color(240f / 255f, 250f / 255f, 1f, 0.95f);
        public Color IceCrack = new Color(1f, 1f, 1f, 0.75f);
        public Color IceBadgeBg = new Color(200f / 255f, 232f / 255f, 1f, 1f);
        public Color IceBadgeText = new Color(0.13f, 0.19f, 0.43f, 1f);

        // ── Aktif tema (Resources fallback, arrowJam deseni) ──────────────────────

        private static HexaThemeData _active;

        public static HexaThemeData Active
        {
            get
            {
                if (_active != null) return _active;
                _active = Resources.Load<HexaThemeData>("Themes/Theme_Default");
                if (_active == null)
                {
                    Debug.LogWarning("[HexaThemeData] Resources/Themes/Theme_Default bulunamadı — " +
                                     "kod-içi varsayılanlar kullanılıyor. Assets ▸ Create ▸ Arrow Rotate ▸ Theme.");
                    _active = CreateInstance<HexaThemeData>();
                }
                return _active;
            }
            set
            {
                if (value == null) return;
                _active = value;
                ApplyCamera();
            }
        }

        /// <summary>Aktif temanın kamera arka planını Main Camera'ya uygular (2D mod).</summary>
        public static void ApplyCamera()
        {
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = Active.CameraBackground;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
