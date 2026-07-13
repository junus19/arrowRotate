using System.Collections.Generic;

namespace ArrowRotate.Core
{
    /// <summary>
    /// Bir level'ın çalışma zamanı durumu. Hücre erişimi tuple-key sözlükle (string key yasak).
    /// Uçuşa başlayan okun hücreleri Cells'ten ANINDA silinir — uçan ok engel sayılmaz.
    /// </summary>
    public sealed class HexaLevel
    {
        public readonly Dictionary<(int q, int r), Cell> Cells = new Dictionary<(int q, int r), Cell>();
        public readonly List<Arrow> Arrows = new List<Arrow>();

        /// <summary>Çözüm halindeki engelleme grafiği: BlockedBy[a] = a'nın ışını üstünde segmenti olan ok kimlikleri.</summary>
        public List<HashSet<int>> BlockedBy = new List<HashSet<int>>();

        public int EdgeCount;
        public int Seed;

        public Cell GetCell(int q, int r)
            => Cells.TryGetValue((q, r), out var c) ? c : null;

        public Cell GetCell((int q, int r) pos)
            => Cells.TryGetValue(pos, out var c) ? c : null;

        public int ExitedCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Arrows.Count; i++) if (Arrows[i].Exited) n++;
                return n;
            }
        }
    }
}
