namespace ArrowRotate.Core
{
    /// <summary>
    /// Üretim konfigürasyonu — zorluk tablosu SKILL.md §7 / prototip levelConfig() birebir.
    /// </summary>
    public sealed class LevelConfig
    {
        public int Radius;
        public int[] Lengths;
        public int MinEdges;
        public int IceCount;          // 0 = buz yok
        public int PaletteCount = 10; // prototip: cfg.colors || 10

        // ── Katman (Layer) üretimi — 1 = düz (klasik), 2-3 = çok katmanlı ──
        public int LayerCount = 1;     // 1..HexaLevel.MaxBuriedLayers+1
        public int SpanningArrows = 0; // parçaları farklı katmanlara yayılacak ok HEDEFİ (best-effort)
        public int BuriedMin = 1;      // yayılan ok başına GÖMÜLÜ parça (layer≥1) alt sınırı
        public int BuriedMax = 2;      // ... üst sınırı (ok başına [min,max]'tan rastgele, [1, len-1]'e kırpılır)

        public int ArrowCount => Lengths.Length;

        public static LevelConfig ForLevel(int n)
        {
            if (n <= 1) return new LevelConfig { Radius = 3, Lengths = new[] { 3, 3, 4 }, MinEdges = 1 };
            if (n == 2) return new LevelConfig { Radius = 4, Lengths = new[] { 3, 4, 5, 6, 6 }, MinEdges = 2 };
            if (n <= 4) return new LevelConfig { Radius = 5, Lengths = new[] { 3, 4, 5, 6, 6, 7 }, MinEdges = 3, IceCount = 3 };
            return new LevelConfig
            {
                Radius = 7,
                Lengths = new[] { 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 10 },
                MinEdges = 6,
                IceCount = 3,
                PaletteCount = 10
            };
        }

        /// <summary>Editör Random Fill için: istenen ok sayısı + katman + yayılma. Uzunluk deseni
        /// "1-2 kısa, kalanı orta-uzun"; yarıçap ok sayısına göre otomatik (katmanlar footprint'i küçültür).</summary>
        public static LevelConfig ForCustom(int arrowCount, int layerCount, int spanningArrows, int iceCount)
            => ForCustom(arrowCount, layerCount, spanningArrows, iceCount, 1, 2);

        public static LevelConfig ForCustom(int arrowCount, int layerCount, int spanningArrows, int iceCount,
                                            int buriedMin, int buriedMax)
        {
            arrowCount = arrowCount < 1 ? 1 : arrowCount;
            layerCount = Clamp(layerCount, 1, HexaLevel.MaxBuriedLayers + 1);

            var lens = new int[arrowCount];
            for (int i = 0; i < arrowCount; i++)
                lens[i] = i < 2 ? 3 : 4 + ((i * 3) % 5); // 2 kısa (3), kalan 4..8 arası çeşitli

            // footprint: toplam hücre / katman → yarıçap. Katman arttıkça aynı alana daha çok hücre sığar.
            int totalCells = 0; foreach (var l in lens) totalCells += l;
            int perLayer = (int)System.Math.Ceiling(totalCells / (double)layerCount);
            int radius = Clamp((int)System.Math.Ceiling(System.Math.Sqrt(perLayer / 2.2)) + 1, 3, 9);

            return new LevelConfig
            {
                Radius = radius,
                Lengths = lens,
                MinEdges = System.Math.Max(1, arrowCount / 3),
                IceCount = Clamp(iceCount, 0, arrowCount),
                PaletteCount = 10,
                LayerCount = layerCount,
                SpanningArrows = Clamp(spanningArrows, 0, arrowCount),
                BuriedMin = System.Math.Max(1, System.Math.Min(buriedMin, buriedMax)),
                BuriedMax = System.Math.Max(System.Math.Max(1, buriedMin), buriedMax),
            };
        }

        private static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
