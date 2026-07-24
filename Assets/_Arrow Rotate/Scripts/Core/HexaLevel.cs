using System.Collections.Generic;

namespace ArrowRotate.Core
{
    /// <summary>
    /// Bir level'ın çalışma zamanı durumu. Hücre erişimi tuple-key sözlükle (string key yasak).
    /// Uçuşa başlayan okun hücreleri Cells'ten ANINDA silinir — uçan ok engel sayılmaz.
    ///
    /// KATMANLAR: Cells SADECE yüzeyi (Layer 0) tutar — ConnectionTracer/RayScanner/tap bilinçli
    /// olarak yalnızca yüzeyi görür (kısmen gömülü ok kendiliğinden bağlanamaz, gömülü hücre engel
    /// sayılmaz). Gömülü hücreler Buried[(q,r)]'de katman sıralı (index 0 = Layer 1) bekler;
    /// yüzey hücresi silinince PromoteAt ile en üstteki terfi eder, kalanların Layer'ı bir azalır.
    /// </summary>
    public sealed class HexaLevel
    {
        /// <summary>Yüzeyin (Layer 0) altında olabilecek en fazla gömülü katman sayısı (Layer 1..2).</summary>
        public const int MaxBuriedLayers = 2;

        public readonly Dictionary<(int q, int r), Cell> Cells = new Dictionary<(int q, int r), Cell>();

        /// <summary>Gömülü hücreler: (q,r) → Layer artan sırada liste (index 0 = Layer 1 = bir sonraki çıkacak).</summary>
        public readonly Dictionary<(int q, int r), List<Cell>> Buried = new Dictionary<(int q, int r), List<Cell>>();

        public readonly List<Arrow> Arrows = new List<Arrow>();

        /// <summary>Anahtar hexagonları: bağımsız hücreler (ok DEĞİL). Bir ok uçuş yolunda üstünden geçince
        /// (çarpınca) tetiklenir, aynı gruptaki kilitli okları açar. Obstacle DEĞİL (ok üstünden uçar).</summary>
        public readonly List<KeyCell> Keys = new List<KeyCell>();

        /// <summary>(q,r)'de henüz tetiklenmemiş anahtar (yoksa null).</summary>
        public KeyCell KeyAt(int q, int r)
        {
            for (int i = 0; i < Keys.Count; i++)
                if (!Keys[i].Triggered && Keys[i].Q == q && Keys[i].R == r) return Keys[i];
            return null;
        }

        /// <summary>Çözüm halindeki engelleme grafiği: BlockedBy[a] = a'nın ışını üstünde segmenti olan ok kimlikleri.</summary>
        public List<HashSet<int>> BlockedBy = new List<HashSet<int>>();

        public int EdgeCount;
        public int Seed;

        public Cell GetCell(int q, int r)
            => Cells.TryGetValue((q, r), out var c) ? c : null;

        public Cell GetCell((int q, int r) pos)
            => Cells.TryGetValue(pos, out var c) ? c : null;

        /// <summary>Hücreyi Layer alanına göre yüzeye ya da gömülü yığına ekler (yükleme/editör içi kullanım).</summary>
        public void AddCell(Cell cell)
        {
            if (cell.Layer <= 0) { Cells[cell.Pos] = cell; return; }
            if (!Buried.TryGetValue(cell.Pos, out var list))
                Buried[cell.Pos] = list = new List<Cell>(MaxBuriedLayers);
            list.Add(cell);
            list.Sort((x, y) => x.Layer.CompareTo(y.Layer));
        }

        /// <summary>Bir okun (q,r)'deki hücresi — önce yüzey, sonra gömülü katmanlar (oklar katmanlara yayılabilir).</summary>
        public Cell GetArrowCell(int arrowId, (int q, int r) pos)
        {
            if (Cells.TryGetValue(pos, out var c) && c.ArrowId == arrowId) return c;
            if (Buried.TryGetValue(pos, out var list))
                for (int i = 0; i < list.Count; i++)
                    if (list[i].ArrowId == arrowId) return list[i];
            return null;
        }

        /// <summary>Okun tüm hücreleri yüzeyde mi? (Bağlanma zaten tracer'la yüzeyde doğal sınırlı;
        /// bu, editör/simülasyon ve UI için açık sorgu.)</summary>
        public bool IsFullySurfaced(Arrow arrow)
        {
            foreach (var pos in arrow.Cells)
            {
                if (!Cells.TryGetValue(pos, out var c) || c.ArrowId != arrow.ArrowId) return false;
            }
            return true;
        }

        /// <summary>(q,r)'deki gömülü yığının en üstünü yüzeye terfi ettirir (Layer 0), kalanları bir katman
        /// yukarı kaydırır. Yüzey DOLUYSA çağırma — önce Cells.Remove. Terfi eden hücreyi döndürür (yoksa null).</summary>
        public Cell PromoteAt((int q, int r) pos)
        {
            if (Cells.ContainsKey(pos)) return null; // yüzey dolu — terfi olmaz
            if (!Buried.TryGetValue(pos, out var list) || list.Count == 0) return null;

            var top = list[0];
            list.RemoveAt(0);
            if (list.Count == 0) Buried.Remove(pos);
            top.Layer = 0;
            Cells[pos] = top;
            for (int i = 0; i < list.Count; i++) list[i].Layer--;
            return top;
        }

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

    /// <summary>Anahtar hexagonu — bağımsız tetikleyici hücre (ok değil). Bir ok üstünden geçince Group kilidini açar.</summary>
    public sealed class KeyCell
    {
        public int Q;
        public int R;
        public int Group;      // eşleşen LockGroup
        public bool Triggered; // runtime: ok çarptı, kilide uçtu
    }
}
