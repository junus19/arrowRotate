using System.Collections.Generic;
using ArrowRotate.Core;

namespace ArrowRotate.Logic
{
    /// <summary>
    /// Uçuş polyline'ı (SKILL.md §11 / prototip buildFlightPath birebir):
    /// kuyruk MERKEZİ → kenar ortaları → her hücrenin MERKEZİ → head ucu (merkez + 0.62·apothem·b).
    /// Merkezden geçme kuralı burada da geçerlidir. Yalnızca ok BAĞLIYKEN çağrılır.
    /// Dönen noktalar Unity dünya koordinatındadır (HexMetrics, y yukarı).
    /// </summary>
    public static class FlightPathBuilder
    {
        public static List<(float x, float y)> Build(HexaLevel level, int arrowId, float s)
        {
            var pts = new List<(float x, float y)>();
            var arrow = level.Arrows[arrowId];
            var tail = level.GetCell(arrow.TailPos);

            var cur = tail;
            int outDir = tail.WorldB;
            pts.Add(HexMetrics.Center(cur.Q, cur.R, s));
            pts.Add(HexMetrics.EdgeMid(cur.Q, cur.R, outDir, s));

            while (true)
            {
                var nx = level.GetCell(HexCoord.Neighbor(cur.Pos, outDir));
                var (cx, cy) = HexMetrics.Center(nx.Q, nx.R, s);
                pts.Add((cx, cy)); // her hücrede merkezden geç

                if (nx.Type == CellType.Head)
                {
                    int flyDir = nx.WorldB;
                    float a = HexMetrics.DirAngleDeg(flyDir) * (float)System.Math.PI / 180f;
                    float tipDist = 0.62f * HexMetrics.Apothem(s);
                    pts.Add((cx + tipDist * (float)System.Math.Cos(a), cy + tipDist * (float)System.Math.Sin(a)));
                    break;
                }

                int d1 = nx.WorldA, d2 = nx.WorldB;
                outDir = d1 == HexCoord.Opp(outDir) ? d2 : d1;
                pts.Add(HexMetrics.EdgeMid(nx.Q, nx.R, outDir, s));
                cur = nx;
            }
            return pts;
        }
    }
}
