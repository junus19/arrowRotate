using UnityEngine;

namespace ArrowJam
{
    /// <summary>
    /// Board görsel teması — hücre arka planları, outline, boş hücre noktası ve
    /// kamera rengi. Yılan renkleri (CellView.PieceColor) temadan bağımsızdır.
    ///
    /// Asset'ler `Assets/Arrow Jam/Resources/Themes/` altında yaşar:
    ///   Theme_Purple (varsayılan) · Theme_Beige · Theme_White
    ///
    /// Aktif tema GridManager inspector'ından atanır; atanmazsa Resources'tan
    /// Theme_Purple yüklenir. Tema değişince ApplyCamera() kamera arka planını
    /// da günceller.
    /// </summary>
    [CreateAssetMenu(fileName = "Theme_New", menuName = "Arrow Jam/Theme")]
    public class ArrowJamThemeData : ScriptableObject
    {
        [Header("Cell Backgrounds")]
        public Color BgFilled    = new(0.18f, 0.18f, 0.28f);
        public Color BgEmpty     = new(0.13f, 0.13f, 0.20f);
        public Color BgDrag      = new(0.26f, 0.26f, 0.38f);
        public Color BgPending   = new(0.18f, 0.18f, 0.28f, 0.50f);
        public Color BgHighlight = new(0.20f, 0.22f, 0.36f); // drag sırasında erişilebilir hücreler

        [Header("Grid")]
        public Color GridColor    = new(0.30f, 0.30f, 0.42f);
        public Color DotColor     = new(0.40f, 0.40f, 0.60f);
        public Color BgGrid       = new(0.13f, 0.13f, 0.20f); // grid background zemini (her hücre tile'ı)
        public Color BgGridShadow = new(0.06f, 0.06f, 0.10f, 0.35f); // grid alt sıra drop shadow (alpha = koyuluk)
        public Color BgGridLine   = new(0.30f, 0.30f, 0.42f); // outline stili grid çizgi rengi

        [Header("Tile")]
        public Color TileShadow = new(0.09f, 0.09f, 0.16f); // blok dip gölgesi (back sprite)

        [Header("Camera")]
        public Color CameraBackground = new(0.10f, 0.10f, 0.14f);

        // ── Active theme ──────────────────────────────────────────────────────────

        private static ArrowJamThemeData _active;

        /// <summary>
        /// Aktif tema. Set edilmemişse Resources'tan Theme_Purple yüklenir;
        /// o da yoksa kod-içi varsayılanlarla geçici instance oluşturulur.
        /// </summary>
        public static ArrowJamThemeData Active
        {
            get
            {
                if (_active != null) return _active;
                _active = Resources.Load<ArrowJamThemeData>("Themes/Theme_Purple");
                if (_active == null)
                {
                    Debug.LogWarning("[ArrowJamThemeData] Theme_Purple not found in Resources/Themes — using built-in defaults.");
                    _active = CreateInstance<ArrowJamThemeData>();
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

        /// <summary>Aktif temanın kamera arka plan rengini Main Camera'ya uygular.</summary>
        public static void ApplyCamera()
        {
            var cam = Camera.main;
            if (cam != null) cam.backgroundColor = Active.CameraBackground;
        }
    }
}
