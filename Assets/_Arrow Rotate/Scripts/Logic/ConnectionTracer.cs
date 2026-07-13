using System.Collections.Generic;
using ArrowRotate.Core;

namespace ArrowRotate.Logic
{
    public readonly struct TraceResult
    {
        public readonly bool Connected;
        public readonly int ExitDir;      // bağlıysa: head'in DÜNYA uçuş yönü
        public readonly Cell HeadCell;

        public TraceResult(bool connected, int exitDir, Cell headCell)
        {
            Connected = connected;
            ExitDir = exitDir;
            HeadCell = headCell;
        }

        public static readonly TraceResult NotConnected = new TraceResult(false, -1, null);
    }

    /// <summary>
    /// Bağlantı tespiti (SKILL.md §4.1 / prototip traceConnected birebir).
    /// Salt okuma — grid'i asla değiştirmez. Her döndürme animasyonu bittikten sonra
    /// yalnızca döndürülen taşın okuna çağrılır.
    /// </summary>
    public static class ConnectionTracer
    {
        public static TraceResult Trace(HexaLevel level, int arrowId)
        {
            var arrow = level.Arrows[arrowId];
            var tail = level.GetCell(arrow.TailPos);
            if (tail == null) return TraceResult.NotConnected;

            var cur = tail;
            int outDir = tail.WorldB;
            int count = 1;
            var visited = new HashSet<(int, int)> { tail.Pos };

            while (true)
            {
                var nPos = HexCoord.Neighbor(cur.Pos, outDir);
                var nx = level.GetCell(nPos);
                if (nx == null || nx.ArrowId != arrowId || visited.Contains(nPos))
                    return TraceResult.NotConnected;

                int need = HexCoord.Opp(outDir);

                if (nx.Type == CellType.Head)
                {
                    if (nx.WorldA != need) return TraceResult.NotConnected;
                    count++;
                    return count == arrow.Len
                        ? new TraceResult(true, nx.WorldB, nx)
                        : TraceResult.NotConnected;
                }

                if (nx.Type != CellType.Mid) return TraceResult.NotConnected;

                // Düz mid'lerde (b−a ≡ 3) iki rot da geçerli bağlantı verebilir — normaldir.
                int d1 = nx.WorldA, d2 = nx.WorldB;
                if (d1 == need) outDir = d2;
                else if (d2 == need) outDir = d1;
                else return TraceResult.NotConnected;

                visited.Add(nPos);
                cur = nx;
                count++;
            }
        }
    }
}
