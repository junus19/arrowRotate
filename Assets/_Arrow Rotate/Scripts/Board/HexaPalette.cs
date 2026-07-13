using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>Sabit 10 renklik palet + zemin renkleri (SKILL.md §6, §9). Tekrar eden renk yasak.</summary>
    public static class HexaPalette
    {
        public static readonly Color Background = FromHex("#5b6285");
        public static readonly Color BackgroundDeep = FromHex("#4e5476");
        public static readonly Color Segment = Color.white;

        public static readonly Color[] Palettes =
        {
            FromHex("#d64545"), // 0 kırmızı
            FromHex("#3d8bfd"), // 1 mavi
            FromHex("#3fae5a"), // 2 yeşil
            FromHex("#ef8f3a"), // 3 turuncu
            FromHex("#a259d6"), // 4 mor
            FromHex("#26b5ac"), // 5 turkuaz
            FromHex("#e0559b"), // 6 pembe
            FromHex("#ecc94b"), // 7 sarı
            FromHex("#96693c"), // 8 kahve
            FromHex("#5f5fe0")  // 9 lacivert
        };

        public static Color ForPalette(int palette) => Palettes[palette % Palettes.Length];

        private static Color FromHex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
