using System;
using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Ok renklerinin (palet) tek kaynağı — arrowJam ColorDatabase deseninin Hexa uyarlaması.
    /// Palet INT ile anahtarlanır (0..N-1): level asset'leri, generator ve spec §6 int kullanır
    /// (arrowJam'deki SnakeColor enum'un aksine). Girdiler dizi sırasına göre indekslenir;
    /// Name yalnızca inspector okunabilirliği içindir.
    ///
    /// Asset `Assets/_Arrow Rotate/Resources/HexaColorDatabase.asset` altında yaşar (otomatik yüklenir).
    /// Oluştur: Assets ▸ Create ▸ Arrow Rotate ▸ Color Database.
    /// </summary>
    [CreateAssetMenu(fileName = "HexaColorDatabase", menuName = "Arrow Rotate/Color Database")]
    public class HexaColorDatabase : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Name;  // sadece inspector etiketi
            public Color Value;
        }

        [Tooltip("Palet renkleri — dizi sırası = palet index'i (0..N-1). Aynı paletteki iki ok komşu olamaz kuralı bu index üzerinden yürür.")]
        [SerializeField] private Entry[] _palettes = DefaultPalettes();

        [Tooltip("Ok segmentlerinin (kuyruk noktası, çizgi, ok ucu) rengi — SKILL §6: beyaz.")]
        public Color SegmentColor = Color.white;

        public int Count => _palettes != null ? _palettes.Length : 0;

        public Color ForPalette(int index)
        {
            if (_palettes == null || _palettes.Length == 0) return Color.white;
            return _palettes[((index % _palettes.Length) + _palettes.Length) % _palettes.Length].Value;
        }

        /// <summary>Editör palet seçici için tüm renkler (sıralı).</summary>
        public Color[] AllColors()
        {
            if (_palettes == null) return Array.Empty<Color>();
            var arr = new Color[_palettes.Length];
            for (int i = 0; i < _palettes.Length; i++) arr[i] = _palettes[i].Value;
            return arr;
        }

        // ── Aktif veritabanı (Resources fallback, arrowJam deseni) ────────────────

        private static HexaColorDatabase _active;

        public static HexaColorDatabase Active
        {
            get
            {
                if (_active != null) return _active;
                _active = Resources.Load<HexaColorDatabase>("HexaColorDatabase");
                if (_active == null)
                {
                    Debug.LogWarning("[HexaColorDatabase] Resources/HexaColorDatabase bulunamadı — " +
                                     "kod-içi varsayılanlar kullanılıyor. Assets ▸ Create ▸ Arrow Rotate ▸ Color Database.");
                    _active = CreateInstance<HexaColorDatabase>();
                }
                return _active;
            }
            set { if (value != null) _active = value; }
        }

        // ── Varsayılanlar (SKILL.md §6 birebir) ──────────────────────────────────

        private static Entry[] DefaultPalettes() => new[]
        {
            new Entry { Name = "0 Kırmızı",  Value = Hex("#d64545") },
            new Entry { Name = "1 Mavi",     Value = Hex("#3d8bfd") },
            new Entry { Name = "2 Yeşil",    Value = Hex("#3fae5a") },
            new Entry { Name = "3 Turuncu",  Value = Hex("#ef8f3a") },
            new Entry { Name = "4 Mor",      Value = Hex("#a259d6") },
            new Entry { Name = "5 Turkuaz",  Value = Hex("#26b5ac") },
            new Entry { Name = "6 Pembe",    Value = Hex("#e0559b") },
            new Entry { Name = "7 Sarı",     Value = Hex("#ecc94b") },
            new Entry { Name = "8 Kahve",    Value = Hex("#96693c") },
            new Entry { Name = "9 Lacivert", Value = Hex("#5f5fe0") },
        };

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
