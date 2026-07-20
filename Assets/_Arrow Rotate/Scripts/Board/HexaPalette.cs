using UnityEngine;

namespace ArrowRotate.View
{
    /// <summary>
    /// Renk erişiminin runtime facade'i. Değerler artık ScriptableObject'lerden okunur:
    /// ok/segment renkleri → <see cref="HexaColorDatabase.Active"/>, zemin → <see cref="HexaThemeData.Active"/>.
    /// Mevcut çağrı yerleri (BoardView, SegmentView, FlightRenderer, HUD, editör) değişmeden çalışır.
    /// </summary>
    public static class HexaPalette
    {
        public static Color Background => HexaThemeData.Active.CameraBackground;
        public static Color Segment => HexaColorDatabase.Active.SegmentColor;

        /// <summary>Editör palet seçici için tüm palet renkleri.</summary>
        public static Color[] Palettes => HexaColorDatabase.Active.AllColors();

        public static Color ForPalette(int palette) => HexaColorDatabase.Active.ForPalette(palette);
    }
}
