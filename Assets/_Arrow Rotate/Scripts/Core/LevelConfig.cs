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
    }
}
