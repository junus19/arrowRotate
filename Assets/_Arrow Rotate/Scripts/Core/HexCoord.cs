using System;

namespace ArrowRotate.Core
{
    /// <summary>
    /// Flat-top hexagon, axial (q, r) koordinat sistemi.
    /// Dirs[d] -> prototip açısı 30 + 60·d (SVG, y AŞAĞI). Unity'ye açı çevirisi HexMetrics'te.
    /// </summary>
    public static class HexCoord
    {
        public static readonly (int dq, int dr)[] Dirs =
        {
            (1, 0),   // 0: sağ-aşağı (proto 30°)
            (0, 1),   // 1: aşağı     (proto 90°)
            (-1, 1),  // 2: sol-aşağı (proto 150°)
            (-1, 0),  // 3: sol-yukarı(proto 210°)
            (0, -1),  // 4: yukarı    (proto 270°)
            (1, -1)   // 5: sağ-yukarı(proto 330°)
        };

        public static int Opp(int d) => (d + 3) % 6;

        public static (int q, int r) Neighbor((int q, int r) pos, int d)
            => (pos.q + Dirs[d].dq, pos.r + Dirs[d].dr);

        /// <summary>a'dan b'ye tek adımlık yön; komşu değilse -1.</summary>
        public static int DirBetween((int q, int r) a, (int q, int r) b)
        {
            for (int d = 0; d < 6; d++)
            {
                if (a.q + Dirs[d].dq == b.q && a.r + Dirs[d].dr == b.r) return d;
            }
            return -1;
        }

        /// <summary>Üretim bölgesi: |q| ≤ R, |r| ≤ R, |q+r| ≤ R.</summary>
        public static bool InRegion(int q, int r, int radius)
            => Math.Abs(q) <= radius && Math.Abs(r) <= radius && Math.Abs(q + r) <= radius;
    }
}
